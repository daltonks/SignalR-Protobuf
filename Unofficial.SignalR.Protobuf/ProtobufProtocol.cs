using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nerdbank.Streams;

namespace Unofficial.SignalR.Protobuf
{
    public class ProtobufProtocol : IHubProtocol
    {
        private readonly IHubProtocol _fallbackProtocol = new MessagePackHubProtocol();
        private readonly List<Type> _types = new List<Type>();
        private readonly Dictionary<Type, int> _messageToIndexMap = new Dictionary<Type, int>();

        public ProtobufProtocol(IEnumerable<Type> messageTypes)
        {
            foreach (var messageType in messageTypes)
            {
                if (!typeof(IMessage).IsAssignableFrom(messageType))
                {
                    throw new ArgumentException($"{messageType} is not a protobuf model ({nameof(IMessage)})");
                }

                _messageToIndexMap[messageType] = _types.Count;
                _types.Add(messageType);
            }
        }

        public string Name => nameof(ProtobufProtocol);
        public int Version => 1;
        public TransferFormat TransferFormat => TransferFormat.Binary;
        public bool IsVersionSupported(int version) => true;
        
        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            return HubProtocolExtensions.GetMessageBytes(this, message);
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            if (message is InvocationMessage invocationMessage)
            {
                var numberOfProtobufModels = invocationMessage.Arguments.Count(argument => argument is IMessage);
                if (numberOfProtobufModels > 0)
                {
                    if (numberOfProtobufModels != invocationMessage.Arguments.Length)
                    {
                        throw new ArgumentException($"{nameof(ProtobufProtocol)} does not currently support a mix of {nameof(IMessage)} and non-{nameof(IMessage)}.");
                    }
                    
                    List<string> headers;
                    if (invocationMessage.Headers == null)
                    {
                        headers = new List<string>(0);
                    }
                    else
                    {
                        headers = new List<string>(invocationMessage.Headers.Count * 2);
                        foreach (var pair in invocationMessage.Headers)
                        {
                            headers.Add(pair.Key);
                            headers.Add(pair.Value);
                        }
                    }

                    var protobufArguments = invocationMessage.Arguments.Cast<IMessage>().ToList();
                    var metadataProtobuf = new InvocationMessageProtobuf
                    {
                        InvocationId = invocationMessage.InvocationId == null 
                            ? null 
                            : new NullableString { Value = invocationMessage.InvocationId },
                        Target = invocationMessage.Target,
                        Headers = { headers },
                        MessageIndices = { 
                            protobufArguments.Select(protobufMessage => _messageToIndexMap[protobufMessage.GetType()])
                        }
                    };

                    var metadataByteCount = metadataProtobuf.CalculateSize();
                    var argumentByteCounts = protobufArguments.Select(argument => argument.CalculateSize()).ToList();

                    using (var outputStream = output.AsStream())
                    {
                        outputStream.WriteByte(1);

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

                    return;
                }
            }
            
            output.Write(new byte[] { 0 });
            _fallbackProtocol.WriteMessage(message, output);
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            // At least one byte is needed to determine if a message should
            // be parsed as Protobuf or as the fallback protocol
            if (input.IsEmpty)
            {
                message = null;
                return false;
            }

            var isProtobuf = input.Slice(0, 1).ToArray()[0] == 1;
            if (isProtobuf)
            {
                // 5 bytes is needed at a minimum to read the 'starting bytes'.
                // The first byte determines if it should be parsed as Protobuf, which has already been read.
                // The next 4 bytes represent the int length of the message.
                if (input.Length < 5)
                {
                    message = null;
                    return false;
                }

                var numberOfBodyBytes = BitConverter.ToInt32(input.Slice(1, 4).ToArray(), 0);
                if (input.Length < 5 + numberOfBodyBytes)
                {
                    message = null;
                    return false;
                }

                input = input.Slice(5);

                using (var inputStream = input.AsStream())
                {
                    var metadataProtobuf = new InvocationMessageProtobuf().MergeFixedDelimitedFrom(inputStream);

                    var protobufMessages = new List<object>();
                    foreach (var messageIndex in metadataProtobuf.MessageIndices)
                    {
                        var protobufMessage = (IMessage) Activator.CreateInstance(_types[messageIndex]);
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
            
            input = input.Slice(1);
            return _fallbackProtocol.TryParseMessage(ref input, binder, out message);
        }
    }
}
