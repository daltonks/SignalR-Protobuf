using System;
using System.Buffers;
using System.Collections.Generic;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nerdbank.Streams;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public class CloseMessageSerializer : IMessageSerializer
    {
        public ProtobufMessageType EnumType => ProtobufMessageType.Close;
        public Type MessageType => typeof(CloseMessage);

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output,
            IReadOnlyDictionary<Type, short> protobufTypeToIndexMap)
        {
            var closeMessage = (CloseMessage) message;

            var protobuf = new CloseMessageProtobuf
            {
                Error = closeMessage.Error
            };

            var bytes = protobuf.ToByteArray();
            // Write number of bytes
            output.Write(BitConverter.GetBytes(bytes.Length));
            // Write bytes
            output.Write(bytes);
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

            CloseMessageProtobuf protobuf;
            using (var inputStream = input.AsStream())
            {
                protobuf = new CloseMessageProtobuf().MergeFixedDelimitedFrom(inputStream);
            }

            input = input.Slice(totalNumberOfBytes);

            message = new CloseMessage(protobuf.Error);
            return true;
        }
    }
}
