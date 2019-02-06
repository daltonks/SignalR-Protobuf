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
        private readonly Dictionary<Type, ushort> _messageToIndexMap = new Dictionary<Type, ushort>();

        public ProtobufProtocol(IReadOnlyList<Type> messageTypes)
        {
            for (ushort i = 0; i < messageTypes.Count; i++)
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
            if (message is InvocationMessage invocationMessage)
            {
                var numberOfProtobufModels = invocationMessage.Arguments.Count(argument => argument is IMessage);
                if (numberOfProtobufModels != invocationMessage.Arguments.Length)
                {
                    throw new ArgumentException($"{nameof(ProtobufProtocol)} does not currently support a mix of {nameof(IMessage)} and non-{nameof(IMessage)}.");
                }
                
                using (var outputStream = output.AsStream())
                using (var binaryWriter = new BinaryWriter(outputStream))
                {
                    // isProtobuf byte
                    binaryWriter.Write((byte) 1);
                    // InvocationId
                    binaryWriter.Write(invocationMessage.InvocationId);
                    // Target
                    binaryWriter.Write(invocationMessage.Target);
                    // Count of Headers
                    binaryWriter.Write((ushort) invocationMessage.Headers.Count);
                    // Header keys and values
                    foreach (var header in invocationMessage.Headers)
                    {
                        binaryWriter.Write(header.Key);
                        binaryWriter.Write(header.Value);
                    }
                    // Count of arguments
                    binaryWriter.Write((byte) invocationMessage.Arguments.Length);

                    var protobufMessages = invocationMessage.Arguments.Cast<IMessage>().ToList();
                    foreach (var protobufMessage in protobufMessages)
                    {
                        // Message index
                        var messageIndex = _messageToIndexMap[message.GetType()];
                        binaryWriter.Write(messageIndex);
                        // Protobuf bytes
                        protobufMessage.WriteDelimitedTo(outputStream);
                    }
                }
            }
            else
            {
                output.Write(new byte[] { 0 });
                _jsonHubProtocol.WriteMessage(message, output);
            }
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            using (var inputStream = input.AsStream())
            using (var binaryReader = new BinaryReader(inputStream))
            {
                var isProtobuf = binaryReader.ReadByte() == 1;

                if (isProtobuf)
                {
                    var invocationId = binaryReader.ReadString();
                    var target = binaryReader.ReadString();
                    var numberOfHeaders = binaryReader.ReadUInt16();
                    var headers = new Dictionary<string, string>();
                    for (var i = 0; i < numberOfHeaders; i++)
                    {
                        var key = binaryReader.ReadString();
                        var value = binaryReader.ReadString();
                        headers[key] = value;
                    }

                    var protobufMessages = new List<object>();
                    var numberOfArguments = binaryReader.ReadByte();
                    for (var i = 0; i < numberOfArguments; i++)
                    {
                        var messageIndex = binaryReader.ReadUInt16();
                        if (messageIndex >= _messageParsers.Count)
                        {
                            message = null;
                            return false;
                        }

                        var protobufMessage = _messageParsers[messageIndex].ParseDelimitedFrom(inputStream);
                        protobufMessages.Add(protobufMessage);
                    }
                    
                    message = new InvocationMessage(invocationId, target, protobufMessages.ToArray())
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
