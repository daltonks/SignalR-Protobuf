using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public interface IMessageSerializer
    {
        IEnumerable<byte> SupportedTypeBytes { get; }
        IEnumerable<Type> SupportedTypes { get; }
        byte GetTypeByte(HubMessage message);
        void WriteMessage(HubMessage message, IBufferWriter<byte> output, IReadOnlyDictionary<Type, int> protobufTypeToIndexMap);
        bool TryParseMessage(ref ReadOnlySequence<byte> input, out HubMessage message, IReadOnlyList<Type> protobufTypes);
    }
}
