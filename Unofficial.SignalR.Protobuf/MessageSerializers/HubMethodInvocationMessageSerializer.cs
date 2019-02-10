using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nerdbank.Streams;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public class HubMethodInvocationMessageSerializer : IMessageSerializer
    {
        public IEnumerable<byte> SupportedTypeBytes => new[]
        {
            ProtobufProtocol.InvocationType,
            ProtobufProtocol.StreamInvocationType
        };

        public IEnumerable<Type> SupportedTypes => new[]
        {
            typeof(InvocationMessage),
            typeof(StreamInvocationMessage)
        };

        public byte GetTypeByte(HubMessage message)
        {
            switch (message)
            {
                case InvocationMessage _:
                    return ProtobufProtocol.InvocationType;
                case StreamInvocationMessage _:
                    return ProtobufProtocol.StreamInvocationType;
                default:
                    throw new ArgumentException($"{message.GetType()} is not mapped in {nameof(HubMethodInvocationMessageSerializer)}");
            }
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output, IReadOnlyDictionary<Type, int> protobufTypeToIndexMap)
        {
            var methodMessage = (HubMethodInvocationMessage) message;

            List<string> headers;
            if (methodMessage.Headers == null)
            {
                headers = new List<string>(0);
            }
            else
            {
                headers = new List<string>(methodMessage.Headers.Count * 2);
                foreach (var pair in methodMessage.Headers)
                {
                    headers.Add(pair.Key);
                    headers.Add(pair.Value);
                }
            }

            var protobufArguments = methodMessage.Arguments.Cast<IMessage>().ToList();
            var messageIndices = protobufArguments.Select(
                protobufMessage => protobufTypeToIndexMap[protobufMessage.GetType()]
            );
            var metadataProtobuf = new InvocationMessageProtobuf
            {
                InvocationId = methodMessage.InvocationId,
                Target = methodMessage.Target,
                Headers = { headers },
                MessageIndices = { messageIndices }
            };

            var metadataByteCount = metadataProtobuf.CalculateSize();
            var argumentByteCounts = protobufArguments.Select(argument => argument.CalculateSize()).ToList();

            using (var outputStream = output.AsStream())
            {
                var totalBodyByteCount = metadataByteCount + argumentByteCounts.Sum() + 4 * (1 + argumentByteCounts.Count);
                outputStream.Write(BitConverter.GetBytes(totalBodyByteCount), 0, 4);

                outputStream.Write(BitConverter.GetBytes(metadataByteCount), 0, 4);
                metadataProtobuf.WriteTo(outputStream);

                for (var i = 0; i < protobufArguments.Count; i++)
                {
                    var protobufArgument = protobufArguments[i];
                    var argumentByteCount = argumentByteCounts[i];
                    outputStream.Write(BitConverter.GetBytes(argumentByteCount), 0, 4);
                    protobufArgument.WriteTo(outputStream);
                }
            }
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, out HubMessage message, IReadOnlyList<Type> protobufTypes)
        {
            // At least 4 bytes are required to read the length of the message
            if (input.Length < 4)
            {
                message = null;
                return false;
            }

            var numberOfBodyBytes = BitConverter.ToInt32(input.Slice(0, 4).ToArray(), 0);
            input = input.Slice(4);

            if (input.Length < numberOfBodyBytes)
            {
                message = null;
                return false;
            }

            using (var inputStream = input.AsStream())
            {
                var metadataProtobuf = new InvocationMessageProtobuf().MergeFixedDelimitedFrom(inputStream);

                var protobufMessages = new List<object>();
                foreach (var messageIndex in metadataProtobuf.MessageIndices)
                {
                    var protobufMessage = (IMessage) Activator.CreateInstance(protobufTypes[messageIndex]);
                    protobufMessage.MergeFixedDelimitedFrom(inputStream);
                    protobufMessages.Add(protobufMessage);
                }

                var headers = new Dictionary<string, string>(metadataProtobuf.Headers.Count / 2);
                for (var i = 0; i <  metadataProtobuf.Headers.Count; i += 2)
                {
                    var key = metadataProtobuf.Headers[i];
                    var value = metadataProtobuf.Headers[i + 1];
                    headers[key] = value;
                }

                message = new InvocationMessage(
                    metadataProtobuf.InvocationId?.Value, 
                    metadataProtobuf.Target, 
                    protobufMessages.ToArray()
                )
                {
                    Headers = headers
                };

                input = input.Slice(numberOfBodyBytes);
                return true;
            }
        }
    }
}
