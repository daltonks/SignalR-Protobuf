using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Spillman.SignalR.Protobuf.MessageSerializers;
using Spillman.SignalR.Protobuf.MessageSerializers.Base;
using Unofficial.SignalR.Protobuf;

namespace Spillman.SignalR.Protobuf
{
    internal enum HubMessageType
    {
        CancelInvocation = 0,
        Close = 1,
        Completion = 2,
        HandshakeRequest = 3,
        HandshakeResponse = 4,
        // InvocationBindingFailure not used, because it is not sent over the wire
        Invocation = 5,
        Ping = 6,
        // StreamBindingFailure not used, because it is not sent over the wire
        StreamInvocation = 7,
        StreamItem = 8
    }

    public class ProtobufProtocol : IHubProtocol
    {
        private static readonly Dictionary<Type, IMessageSerializer> TypeToSerializerMap = new Dictionary<Type, IMessageSerializer>();
        private static readonly Dictionary<HubMessageType, IMessageSerializer> EnumTypeToSerializerMap = new Dictionary<HubMessageType, IMessageSerializer>();

        static ProtobufProtocol()
        {
            var serializers = new IMessageSerializer[]
            {
                new CancelInvocationMessageSerializer(), 
                new CloseMessageSerializer(), 
                new CompletionMessageSerializer(),
                new HandshakeRequestMessageSerializer(), 
                new HandshakeResponseMessageSerializer(), 
                new InvocationMessageSerializer(), 
                new PingMessageSerializer(), 
                new StreamInvocationMessageSerializer(), 
                new StreamItemMessageSerializer()
            };

            foreach (var serializer in serializers)
            {
                EnumTypeToSerializerMap[serializer.HubMessageType] = serializer;
                TypeToSerializerMap[serializer.MessageType] = serializer;
            }
        }
        
        private readonly Dictionary<int, Type> _protobufIndexToTypeMap = new Dictionary<int, Type>();
        private readonly Dictionary<Type, int> _protobufTypeToIndexMap = new Dictionary<Type, int>();
        
        public ProtobufProtocol(IReadOnlyDictionary<int, Type> protobufTypes)
        {
            foreach (var pair in protobufTypes)
            {
                var index = pair.Key;
                var type = pair.Value;

                if (index < 0)
                {
                    throw new ArgumentException(
                        $"Index \"{index}\" for type {type} is less than 0",
                        nameof(protobufTypes)
                    );
                }

                if (!typeof(IMessage).IsAssignableFrom(type))
                {
                    throw new ArgumentException(
                        $"{type} is not a protobuf model ({nameof(IMessage)})",
                        nameof(protobufTypes)
                    );
                }
            }
            
            var allPairs = protobufTypes
                .Select(pair => (pair.Key, pair.Value))
                .Concat(
                    new[]
                    {
                        // Leave a 64 int gap for special type cases
                        // (ex: nulls and enumerables)
                        (-65, typeof(MessageMetadata)),
                        (-66, typeof(ItemMetadata)),
                        (-67, typeof(CancelInvocationMessageProtobuf)),
                        (-68, typeof(CloseMessageProtobuf)),
                        (-69, typeof(CompletionMessageProtobuf)),
                        (-70, typeof(HandshakeRequestMessageProtobuf)),
                        (-71, typeof(HandshakeResponseMessageProtobuf)),
                        (-72, typeof(InvocationMessageProtobuf)),
                        (-73, typeof(StreamInvocationMessageProtobuf)),
                        (-74, typeof(StreamItemMessageProtobuf)),
                        (-75, typeof(NullableString))
                    }
                );
            foreach (var (index, type) in allPairs)
            {
                _protobufIndexToTypeMap[index] = type;
                _protobufTypeToIndexMap[type] = index;
            }

            foreach (var modelsMessageType in ModelsReflection.Descriptor.MessageTypes)
            {
                var clrType = modelsMessageType.ClrType;
                if (!_protobufTypeToIndexMap.ContainsKey(clrType))
                {
                    throw new Exception($"{clrType} is not mapped in {nameof(ProtobufProtocol)}");
                }
            }
        }

        public string Name => nameof(ProtobufProtocol);
        public int Version => 3;
        public TransferFormat TransferFormat => TransferFormat.Binary;
        public bool IsVersionSupported(int version) => version == Version;
        
        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            return HubProtocolExtensions.GetMessageBytes(this, message);
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            var serializer = TypeToSerializerMap[message.GetType()];
            output.Write(new[] { (byte) serializer.HubMessageType });
            serializer.WriteMessage(message, output, _protobufTypeToIndexMap);
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            // At least one byte is needed to determine the type of message
            if (input.IsEmpty)
            {
                message = null;
                return false;
            }

            var enumType = (HubMessageType) input.Slice(0, 1).ToArray()[0];
            var processedSequence = input.Slice(1);
            
            var serializer = EnumTypeToSerializerMap[enumType];
            var successfullyParsed = serializer.TryParseMessage(
                ref processedSequence, 
                out message, 
                _protobufIndexToTypeMap
            );

            if (successfullyParsed)
            {
                input = processedSequence;
            }

            return successfullyParsed;
        }
    }
}
