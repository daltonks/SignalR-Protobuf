using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Spillman.SignalR.Protobuf.Util;
using Unofficial.SignalR.Protobuf;

namespace Spillman.SignalR.Protobuf.MessageSerializers.Base
{
    internal abstract class BaseMessageSerializer : IMessageSerializer
    {
        public abstract HubMessageType HubMessageType { get; }
        public abstract Type MessageType { get; }

        protected abstract IEnumerable<object> CreateItems(HubMessage message);
        protected abstract HubMessage CreateHubMessage(IReadOnlyList<object> items, Exception bindingException);

        public void WriteMessage(
            HubMessage message,
            IBufferWriter<byte> output,
            IReadOnlyDictionary<Type, int> protobufTypeToIndexMap
        )
        {
            var itemsMetadata = CreateItems(message)
                .Select(item => ItemMetadata.Create(item, protobufTypeToIndexMap))
                .ToList();

            var metadata = new MessageMetadata
            {
                Items = {itemsMetadata}
            };

            var metadataByteSize = metadata.CalculateSize();

            var totalByteSize = 4 // Total byte size (int)
                                + 4 + metadataByteSize // Metadata byte size (int) and metadata itself
                                + itemsMetadata
                                    .Select(itemMetadata => itemMetadata.CalculateTotalSizeBytes())
                                    .Sum(); // Total bytes of the protobuf models

            var byteArray = ArrayPool<byte>.Shared.Rent(totalByteSize);
            try
            {
                using (var outputStream = new MemoryStream(byteArray))
                {
                    // Total byte size
                    outputStream.Write(BitConverter.GetBytes(totalByteSize), 0, 4);

                    // Metadata byte size
                    outputStream.Write(BitConverter.GetBytes(metadataByteSize), 0, 4);

                    // Metadata
                    metadata.WriteTo(outputStream);

                    // Other protobufs
                    foreach (var protobufItem in metadata.Items.SelectMany(item => item.NonNullProtobufs))
                    {
                        protobufItem.WriteTo(outputStream);
                    }
                }

                output.Write(new ReadOnlySpan<byte>(byteArray, 0, totalByteSize));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }

        public bool TryParseMessage(
            ref ReadOnlySequence<byte> input,
            out HubMessage message,
            IReadOnlyDictionary<int, Type> protobufIndexToTypeMap
        )
        {
            // 4 bytes are required to read the length of the message
            if (input.Length < 4)
            {
                message = null;
                return false;
            }

            var totalByteSize = BitConverter.ToInt32(input.Slice(0, 4).ToArray(), 0);
            if (input.Length < totalByteSize)
            {
                message = null;
                return false;
            }

            var protobufInput = input.Slice(4);

            var byteArray = ArrayPool<byte>.Shared.Rent((int) protobufInput.Length);
            try
            {
                protobufInput.CopyTo(byteArray);

                using (var inputStream = new MemoryStream(byteArray))
                {
                    var metadata = new MessageMetadata();
                    metadata.MergeFixedDelimitedFrom(inputStream);

                    Exception bindingException = null;

                    var items = new List<object>();
                    foreach (var itemMetadata in metadata.Items)
                    {
                        try
                        {
                            var (error, item) = itemMetadata.CreateItem(inputStream, protobufIndexToTypeMap);
                            if (error == null)
                            {
                                items.Add(item);
                            }
                            else
                            {
                                bindingException = new Exception(error);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            bindingException = ex;
                            break;
                        }
                    }

                    message = CreateHubMessage(items, bindingException);
                }

                input = input.Slice(totalByteSize);

                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }
    }
}