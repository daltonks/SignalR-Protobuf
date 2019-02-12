using System;
using System.Buffers;
using System.Collections.Generic;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nerdbank.Streams;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public class HandshakeResponseMessageSerializer : IMessageSerializer
    {
        public ProtobufMessageType EnumType => ProtobufMessageType.HandshakeResponse;
        public Type MessageType => typeof(HandshakeResponseMessage);

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output,
            IReadOnlyDictionary<Type, short> protobufTypeToIndexMap)
        {
            var handshakeResponseMessage = (HandshakeResponseMessage) message;

            output.WriteMessage(
                new HandshakeResponseMessageProtobuf
                {
                    Error = handshakeResponseMessage.Error
                },
                protobufTypeToIndexMap
            );
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, out HubMessage message, IReadOnlyList<Type> protobufTypes)
        {
            if (input.Length < 4)
            {
                message = null;
                return false;
            }

            var numberOfBodyBytes = BitConverter.ToInt32(input.Slice(0, 4).ToArray(), 0);
            var totalNumberOfBytes = 4 + numberOfBodyBytes;

            if (input.Length < totalNumberOfBytes)
            {
                message = null;
                return false;
            }

            HandshakeResponseMessageProtobuf protobuf;
            using (var inputStream = input.AsStream())
            {
                protobuf = new HandshakeResponseMessageProtobuf().MergeFixedDelimitedFrom(inputStream);
            }

            input = input.Slice(totalNumberOfBytes);

            message = new HandshakeResponseMessage(protobuf.Error);
            return true;
        }
    }
}
