using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly ConcurrentDictionary<Type, object> TypeToParserMap = new ConcurrentDictionary<Type, object>();
        private static MessageParser GetParser(Type type)
        {
            if(!TypeToParserMap.TryGetValue(type, out var parser))
            {
                TypeToParserMap[type] 
                    = parser 
                        = type.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            }

            return (MessageParser) parser;
        }
        
        private readonly JsonHubProtocol _jsonHubProtocol = new JsonHubProtocol();
        private readonly List<MessageParser> _messageParsers = new List<MessageParser>();
        private readonly Dictionary<Type, int> _messageToIndexMap = new Dictionary<Type, int>();

        public ProtobufProtocol(IReadOnlyList<Type> messageTypes)
        {
            for (var i = 0; i < messageTypes.Count; i++)
            {
                var messageType = messageTypes[i];

                if (!typeof(IMessage).IsAssignableFrom(messageType))
                {
                    continue;
                }

                _messageParsers.Add(GetParser(messageType));
                _messageToIndexMap[messageType] = i;
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
            var writeAsJson = true;
            if (message is InvocationMessage invocationMessage)
            {
                var numberOfProtobufModels = invocationMessage.Arguments.Count(argument => argument is IMessage);
                if (numberOfProtobufModels > 0)
                {
                    writeAsJson = false;

                    if (numberOfProtobufModels != invocationMessage.Arguments.Length)
                    {
                        throw new ArgumentException($"{nameof(ProtobufProtocol)} does not currently support a mix of {nameof(IMessage)} and non-{nameof(IMessage)}.");
                    }

                    using (var outputStream = output.AsStream())
                    {
                        outputStream.WriteByte(1);

                        var protobufArguments = invocationMessage.Arguments.Cast<IMessage>().ToList();

                        List<string> headers;
                        if (invocationMessage.Headers == null)
                        {
                            headers = new List<string>(0);
                        }
                        else
                        {
                            headers = new List<string>(invocationMessage.Headers.Count);
                            foreach (var pair in invocationMessage.Headers)
                            {
                                headers.Add(pair.Key);
                                headers.Add(pair.Value);
                            }
                        }

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

                        metadataProtobuf.WriteDelimitedTo(outputStream);
                        
                        foreach (var protobufArgument in protobufArguments)
                        {
                            protobufArgument.WriteDelimitedTo(outputStream);
                        }
                    }
                }
            }
            
            if(writeAsJson)
            {
                output.Write(new byte[] { 0 });
                _jsonHubProtocol.WriteMessage(message, output);
            }
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            using (var inputStream = input.AsStream())
            {
                var isProtobuf = inputStream.ReadByte() == 1;

                if (isProtobuf)
                {
                    var metadataProtobuf = InvocationMessageProtobuf.Parser.ParseDelimitedFrom(inputStream);

                    var protobufMessages = new List<object>();
                    foreach (var messageIndex in metadataProtobuf.MessageIndices)
                    {
                        var protobufMessage = _messageParsers[messageIndex].ParseDelimitedFrom(inputStream);
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

                    return true;
                }
            }

            var jsonSequence = input.Slice(1);
            return _jsonHubProtocol.TryParseMessage(ref jsonSequence, binder, out message);
        }
    }
}
