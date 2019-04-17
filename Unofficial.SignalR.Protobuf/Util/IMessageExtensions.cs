using System;
using System.IO;
using Google.Protobuf;

namespace Unofficial.SignalR.Protobuf.Util
{
    // ReSharper disable once InconsistentNaming
    internal static class IMessageExtensions
    {
        internal static void MergeFixedDelimitedFrom<T>(this T protobufMessage, Stream stream) where T : IMessage
        {
            var lengthBytes = new byte[4];
            stream.Read(lengthBytes, 0, 4);

            var numberOfBytes = BitConverter.ToInt32(lengthBytes, 0);
            protobufMessage.MergeFrom(stream, numberOfBytes);
        }

        internal static void MergeFrom<T>(this T protobufMessage, Stream stream, int numberOfBytes) where T : IMessage
        {
            using (var limitedInputStream = new LimitedInputStream(stream, numberOfBytes))
            {
                protobufMessage.MergeFrom(limitedInputStream);
            }
        }
    }
}
