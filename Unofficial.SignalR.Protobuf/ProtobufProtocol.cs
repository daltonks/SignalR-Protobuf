using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nerdbank.Streams;
using Unofficial.SignalR.Protobuf.MessageSerializers;

namespace Unofficial.SignalR.Protobuf
{
    public class ProtobufProtocol : IHubProtocol
    {
        public const byte InvocationType = 0;
        public const byte StreamInvocationType = 1;
        public const byte StreamItemType = 2;
        public const byte FallbackType = 3;

        private static readonly Dictionary<Type, IMessageSerializer> TypeToSerializerMap = new Dictionary<Type, IMessageSerializer>();
        private static readonly Dictionary<byte, IMessageSerializer> TypeByteToSerializerMap = new Dictionary<byte, IMessageSerializer>();

        static ProtobufProtocol()
        {
            var serializers = new IMessageSerializer[]
            {
                new HubMethodInvocationMessageSerializer(), 
                new StreamItemMessageSerializer()
            };

            foreach (var serializer in serializers)
            {
                foreach (var supportedType in serializer.SupportedTypes)
                {
                    TypeToSerializerMap[supportedType] = serializer;
                }

                foreach (var supportedTypeByte in serializer.SupportedTypeBytes)
                {
                    TypeByteToSerializerMap[supportedTypeByte] = serializer;
                }
            }
        }
        
        private readonly List<Type> _protobufTypes = new List<Type>();
        private readonly Dictionary<Type, int> _protobufTypeToIndexMap = new Dictionary<Type, int>();
        private readonly IHubProtocol _fallbackProtocol = new MessagePackHubProtocol();

        public ProtobufProtocol(IEnumerable<Type> protobufTypes)
        {
            foreach (var protobufType in protobufTypes)
            {
                if (!typeof(IMessage).IsAssignableFrom(protobufType))
                {
                    throw new ArgumentException($"{protobufType} is not a protobuf model ({nameof(IMessage)})");
                }

                _protobufTypeToIndexMap[protobufType] = _protobufTypes.Count;
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
            if (TypeToSerializerMap.TryGetValue(message.GetType(), out var serializer))
            {
                var typeByte = serializer.GetTypeByte(message);
                output.Write(new[] { typeByte });
                serializer.WriteMessage(message, output, _protobufTypeToIndexMap);
            }
            else
            {
                output.Write(new [] { FallbackType });
                _fallbackProtocol.WriteMessage(message, output);
            }
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            // At least one byte is needed to determine the type of message
            if (input.IsEmpty)
            {
                message = null;
                return false;
            }

            var typeByte = input.Slice(0, 1).ToArray()[0];
            var processedSequence = input.Slice(1);

            var successfullyParsed = TypeByteToSerializerMap.TryGetValue(typeByte, out var serializer) 
                ? serializer.TryParseMessage(ref processedSequence, out message, typeByte, _protobufTypes) 
                : _fallbackProtocol.TryParseMessage(ref processedSequence, binder, out message);

            if (successfullyParsed)
            {
                input = processedSequence;
            }

            return successfullyParsed;
        }
    }
}
