using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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
        protected abstract HubMessage CreateHubMessage(IReadOnlyList<object> protobufModels);

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output, IReadOnlyDictionary<Type, short> protobufTypeToIndexMap)
        {
            var protobufModels = CreateProtobufModels(message);

            // Calculate byte sizes. If null, the byte size is 0.
            var byteSizes = protobufModels
                .Select(protobufModel => protobufModel?.CalculateSize() ?? 0)
                .ToList();

            // Body byte size includes the ints of protobuf model sizes
            // and the shorts of their type index
            var bodyByteSize = 6 * protobufModels.Count + byteSizes.Sum();

            using (var outputStream = output.AsStream())
            {
                // Write total byte size
                outputStream.Write(BitConverter.GetBytes(bodyByteSize), 0, 4);

                // Write number of protobuf models
                outputStream.WriteByte((byte) protobufModels.Count);

                for (var i = 0; i < protobufModels.Count; i++)
                {
                    var protobufModel = protobufModels[i];
                    var byteSize = byteSizes[i];

                    var typeShort = protobufModel == null 
                        ? (short) -1 
                        : protobufTypeToIndexMap[protobufModel.GetType()];

                    // Write the model's type
                    outputStream.Write(BitConverter.GetBytes(typeShort), 0, 2);

                    // Write the model's byte size
                    outputStream.Write(BitConverter.GetBytes(byteSize), 0, 4);

                    if (protobufModel != null)
                    {
                        // Write the model type
                        var typeIndex = protobufTypeToIndexMap[protobufModel.GetType()];
                        outputStream.Write(BitConverter.GetBytes(typeIndex), 0, 2);

                        // Write the model itself
                        protobufModel.WriteTo(outputStream);
                    }
                }
            }
        }
        
        public bool TryParseMessage(ref ReadOnlySequence<byte> input, out HubMessage message, IReadOnlyList<Type> protobufTypes)
        {
            // At least 5 bytes are required to read the length of the message
            // and the number of protobuf models
            if (input.Length < 5)
            {
                message = null;
                return false;
            }

            var numberOfBodyBytes = BitConverter.ToInt32(input.Slice(0, 4).ToArray(), 0);
            input = input.Slice(4);

            var numberOfProtobufModels = input.Slice(0, 1).ToArray()[0];
            input = input.Slice(1);

            if (input.Length < numberOfBodyBytes)
            {
                message = null;
                return false;
            }

            using (var inputStream = input.AsStream())
            {
                var protobufModels = new object[numberOfProtobufModels];
                for (var i = 0; i < numberOfProtobufModels; i++)
                {
                    var typeShortBytes = new byte[2];
                    inputStream.Read(typeShortBytes, 0, 2);
                    var typeIndex = BitConverter.ToInt16(typeShortBytes, 0);

                    if (typeIndex == -1)
                    {
                        // Skip size bytes, because the model is null
                        inputStream.Seek(4, SeekOrigin.Current);
                    }
                    else
                    {
                        var protobufModel = (IMessage) Activator.CreateInstance(protobufTypes[typeIndex]);
                        protobufModel.MergeFixedDelimitedFrom(inputStream);
                        protobufModels[i] = protobufModel;
                    }
                }

                message = CreateHubMessage(protobufModels);
                return true;
            }
        }
    }
}
