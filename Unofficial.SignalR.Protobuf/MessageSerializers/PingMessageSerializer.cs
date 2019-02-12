using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public class PingMessageSerializer : IMessageSerializer
    {
        public ProtobufMessageType EnumType => ProtobufMessageType.Ping;
        public Type MessageType => typeof(PingMessage);

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output,
            IReadOnlyDictionary<Type, short> protobufTypeToIndexMap)
        {
            
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, out HubMessage message, IReadOnlyList<Type> protobufTypes)
        {
            message = PingMessage.Instance;
            return true;
        }
    }
}
