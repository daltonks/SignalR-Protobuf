using System;
using System.Buffers;
using System.Collections.Generic;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nerdbank.Streams;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public class StreamItemMessageSerializer : IMessageSerializer
    {
        public IEnumerable<byte> SupportedTypeBytes => new[]
        {
            ProtobufProtocol.StreamItemType
        };

        public IEnumerable<Type> SupportedTypes => new[]
        {
            typeof(StreamItemMessage)
        };

        public byte GetTypeByte(HubMessage message)
        {
            return ProtobufProtocol.StreamItemType;
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output, IReadOnlyDictionary<Type, int> protobufTypeToIndexMap)
        {
            var streamItemMessage = (StreamItemMessage) message;

            var item = streamItemMessage.Item;
            var protobufItem = (IMessage) item;
            var metadataProtobuf = new StreamItemMessageProtobuf
            {
                InvocationId = streamItemMessage.InvocationId,
                Headers = { streamItemMessage.Headers.Flatten() },
                MessageIndex = protobufTypeToIndexMap[item.GetType()]
            };

            var metadataByteCount = metadataProtobuf.CalculateSize();
            var itemByteCount = protobufItem.CalculateSize();
            var totalByteCount = metadataByteCount + itemByteCount + 8;

            using (var outputStream = output.AsStream())
            {
                outputStream.Write(BitConverter.GetBytes(totalByteCount), 0, 4);

                outputStream.Write(BitConverter.GetBytes(metadataByteCount), 0, 4);
                metadataProtobuf.WriteTo(outputStream);

                outputStream.Write(BitConverter.GetBytes(itemByteCount), 0, 4);
                protobufItem.WriteTo(outputStream);
            }
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, out HubMessage message, byte typeByte, IReadOnlyList<Type> protobufTypes)
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
                var metadataProtobuf = new StreamItemMessageProtobuf().MergeFixedDelimitedFrom(inputStream);

                var protobufItem = (IMessage) Activator.CreateInstance(protobufTypes[metadataProtobuf.MessageIndex]);
                protobufItem.MergeFixedDelimitedFrom(inputStream);

                message = new StreamItemMessage(metadataProtobuf.InvocationId, protobufItem)
                {
                    Headers = metadataProtobuf.Headers.Unflatten()
                };

                input = input.Slice(numberOfBodyBytes);
                return true;
            }
        }
    }
}
