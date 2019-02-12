using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nerdbank.Streams;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public abstract class BaseMessageSerializer : IMessageSerializer
    {
        public abstract ProtobufMessageType EnumType { get; }
        public abstract Type MessageType { get; }

        protected abstract IReadOnlyList<IMessage> CreateProtobufModels(HubMessage message);
        protected abstract HubMessage CreateHubMessage(IReadOnlyList<IMessage> protobufModels);

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output, IReadOnlyDictionary<Type, short> protobufTypeToIndexMap)
        {
            var protobufModels = CreateProtobufModels(message);

            var numberOfNullProtobufModels = protobufModels.Count(protobufModel => protobufModel == null);
            var numberOfNonNullProtobufModels = protobufModels.Count - numberOfNullProtobufModels;

            // Calculate byte sizes
            var protobufByteSizes = protobufModels
                .Select(protobufModel => protobufModel?.CalculateSize() ?? 0)
                .ToList();

            var totalByteSize = 4 // Total byte size (int)
                              + 1 // Number of protobuf models (byte)
                              + 2 * numberOfNullProtobufModels // Type (short) per null protobuf model
                              + (2 + 4) * numberOfNonNullProtobufModels // Type (short) and byte size (int) per non-null protobuf model
                              + protobufByteSizes.Sum(); // Total bytes of the protobuf models themselves

            using (var outputStream = output.AsStream())
            {
                // Total byte size
                outputStream.Write(BitConverter.GetBytes(totalByteSize), 0, 4);

                // Number of protobuf models
                outputStream.WriteByte((byte) protobufModels.Count);

                for (var i = 0; i < protobufModels.Count; i++)
                {
                    var protobufModel = protobufModels[i];
                    var byteSize = protobufByteSizes[i];

                    if (protobufModel == null)
                    {
                        // Type: -1
                        outputStream.Write(BitConverter.GetBytes((short) -1), 0, 2);
                    }
                    else
                    {
                        var typeShort = protobufTypeToIndexMap[protobufModel.GetType()];

                        // Type
                        outputStream.Write(BitConverter.GetBytes(typeShort), 0, 2);

                        // Byte Size
                        outputStream.Write(BitConverter.GetBytes(byteSize), 0, 4);

                        // Model
                        protobufModel.WriteTo(outputStream);
                    }
                }
            }
        }
        
        public bool TryParseMessage(ref ReadOnlySequence<byte> input, out HubMessage message, IReadOnlyList<Type> protobufTypes)
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

            input = input.Slice(4);

            var numberOfProtobufModels = input.Slice(0, 1).ToArray()[0];
            input = input.Slice(1);
            
            var protobufModels = new IMessage[numberOfProtobufModels];

            using (var inputStream = input.AsStream())
            {
                for (var i = 0; i < numberOfProtobufModels; i++)
                {
                    var typeShortBytes = new byte[2];
                    inputStream.Read(typeShortBytes, 0, 2);
                    var typeIndex = BitConverter.ToInt16(typeShortBytes, 0);

                    if (typeIndex != -1)
                    {
                        var protobufModel = (IMessage) Activator.CreateInstance(protobufTypes[typeIndex]);
                        protobufModel.MergeFixedDelimitedFrom(inputStream);
                        protobufModels[i] = protobufModel;
                    }
                }
            }

            message = CreateHubMessage(protobufModels);
            return true;
        }
    }
}
