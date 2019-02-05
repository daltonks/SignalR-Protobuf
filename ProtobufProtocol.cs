using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public string Name => nameof(ProtobufProtocol);
        public int Version => 1;
        public TransferFormat TransferFormat => TransferFormat.Binary;
        public bool IsVersionSupported(int version) => true;

        private readonly JsonHubProtocol _jsonHubProtocol = new JsonHubProtocol();
        private readonly List<MessageParser> _messageParsers = new List<MessageParser>();
        private readonly Dictionary<Type, ushort> _messageToIndexMap = new Dictionary<Type, ushort>();

        public ProtobufProtocol(IReadOnlyList<Type> messageTypes)
        {
            foreach (var messageType in messageTypes)
            {
                if (!messageType.IsSubclassOf(typeof(IMessage)))
                {
                    throw new ArgumentException($"{messageType} does not implement {nameof(IMessage)}");
                }

                if (!messageType.IsSubclassOf(typeof(HubMessage)))
                {
                    throw new ArgumentException($"{messageType} does not extend from {nameof(HubMessage)}");
                }
            }

            for (ushort i = 0; i < messageTypes.Count; i++)
            {
                var messageType = messageTypes[i];
                _messageParsers.Add(GetParser(messageType));
                _messageToIndexMap[messageType] = i;
            }
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            if (message is IMessage protobufMessage)
            {
                using (var outputStream = output.AsStream())
                {
                    outputStream.WriteByte(0);

                    var messageIndex = _messageToIndexMap[message.GetType()];
                    var indexBytes = BitConverter.GetBytes(messageIndex);
                    outputStream.WriteByte(indexBytes[0]);
                    outputStream.WriteByte(indexBytes[1]);
                    protobufMessage.WriteTo(outputStream);
                }
            }
            else
            {
                output.Write(new byte[] { 1 });
                _jsonHubProtocol.WriteMessage(message, output);
            }
        }

        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            if (message is IMessage protobufMessage)
            {
                var messageIndex = _messageToIndexMap[message.GetType()];
                var messageIndexBytes = BitConverter.GetBytes(messageIndex);

                var messageBytes = protobufMessage.ToByteArray();

                var combinedBytes = new byte[3 + messageBytes.Length];

                combinedBytes[0] = 0;
                combinedBytes[1] = messageIndexBytes[0];
                combinedBytes[2] = messageIndexBytes[1];

                Array.Copy(messageBytes, 0, combinedBytes, 3, messageBytes.Length);

                return combinedBytes;
            }
            else
            {
                var jsonBytes = _jsonHubProtocol.GetMessageBytes(message).ToArray();

                var combinedBytes = new byte[1 + jsonBytes.Length];
                combinedBytes[0] = 1;
                
                Array.Copy(jsonBytes, 0, combinedBytes, 1, jsonBytes.Length);

                return combinedBytes;
            }
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            var isProtobuf = input.Slice(0, 1).ToArray()[0] == 0;

            if (isProtobuf)
            {
                var indexBytes = input.Slice(1, 2).ToArray();
                var index = BitConverter.ToUInt16(indexBytes, 0);
                if (index >= _messageParsers.Count)
                {
                    message = null;
                    return false;
                }

                var messageSequence = input.Slice(3);
                using (var messageStream = messageSequence.AsStream())
                {
                    message = (HubMessage) _messageParsers[index].ParseFrom(messageStream);
                    return true;
                }
            }
            else
            {
                var jsonSequence = input.Slice(1);
                return _jsonHubProtocol.TryParseMessage(ref jsonSequence, binder, out message);
            }
        }
    }
}
