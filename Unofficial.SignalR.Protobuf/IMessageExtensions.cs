using System;
using System.IO;
using Google.Protobuf;

namespace Unofficial.SignalR.Protobuf
{
    // ReSharper disable once InconsistentNaming
    public static class IMessageExtensions
    {
        public static T MergeFixedDelimitedFrom<T>(this T protobufMessage, Stream stream) where T : IMessage
        {
            var lengthBytes = new byte[4];
            stream.Read(lengthBytes, 0, 4);

            var numberOfBytes = BitConverter.ToInt32(lengthBytes, 0);
            using (var limitedInputStream = new LimitedInputStream(stream, numberOfBytes))
            {
                protobufMessage.MergeFrom(limitedInputStream);
            }

            return protobufMessage;
        }
    }
}
