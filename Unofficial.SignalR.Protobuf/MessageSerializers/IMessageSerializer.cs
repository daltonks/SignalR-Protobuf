using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public interface IMessageSerializer
    {
        ProtobufMessageType EnumType { get; }
        Type MessageType { get; }
        void WriteMessage(HubMessage message, IBufferWriter<byte> output, IReadOnlyDictionary<Type, short> protobufTypeToIndexMap);
        bool TryParseMessage(ref ReadOnlySequence<byte> input, out HubMessage message, IReadOnlyList<Type> protobufTypes);
    }
}
