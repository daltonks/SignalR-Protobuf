using System;
using System.Buffers;
using System.Collections.Generic;
using Google.Protobuf;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers;

namespace Unofficial.SignalR.Protobuf
{
    public enum ProtobufMessageType
    {
        Close,
        HandshakeResponse,
        Invocation,
        Ping,
        StreamInvocation,
        StreamItem
    }

    public class ProtobufProtocol : IHubProtocol
    {
        private static readonly Dictionary<Type, IMessageSerializer> TypeToSerializerMap = new Dictionary<Type, IMessageSerializer>();
        private static readonly Dictionary<ProtobufMessageType, IMessageSerializer> EnumTypeToSerializerMap = new Dictionary<ProtobufMessageType, IMessageSerializer>();

        static ProtobufProtocol()
        {
            var serializers = new IMessageSerializer[]
            {
                new CloseMessageSerializer(), 
                new HandshakeResponseMessageSerializer(), 
                new InvocationMessageSerializer(), 
                new PingMessageSerializer(), 
                new StreamInvocationMessageSerializer(), 
                new StreamItemMessageSerializer()
            };

            foreach (var serializer in serializers)
            {
                EnumTypeToSerializerMap[serializer.EnumType] = serializer;
                TypeToSerializerMap[serializer.MessageType] = serializer;
            }
        }
        
        private readonly List<Type> _protobufTypes = new List<Type>();
        private readonly Dictionary<Type, short> _protobufTypeToIndexMap = new Dictionary<Type, short>();

        public ProtobufProtocol(IEnumerable<Type> protobufTypes)
        {
            foreach (var protobufType in protobufTypes)
            {
                if (!typeof(IMessage).IsAssignableFrom(protobufType))
                {
                    throw new ArgumentException($"{protobufType} is not a protobuf model ({nameof(IMessage)})");
                }

                _protobufTypeToIndexMap[protobufType] = (short) _protobufTypes.Count;
                _protobufTypes.Add(protobufType);
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
            var serializer = TypeToSerializerMap[message.GetType()];
            output.Write(new[] { (byte) serializer.EnumType });
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

            var enumType = (ProtobufMessageType) input.Slice(0, 1).ToArray()[0];
            var processedSequence = input.Slice(1);

            var serializer = EnumTypeToSerializerMap[enumType];
            var successfullyParsed = serializer.TryParseMessage(ref processedSequence, out message, _protobufTypes);

            if (successfullyParsed)
            {
                input = processedSequence;
            }

            return successfullyParsed;
        }
    }
}
