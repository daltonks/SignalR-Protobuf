using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using Spillman.SignalR.Protobuf.Util;

// ReSharper disable once CheckNamespace
namespace Unofficial.SignalR.Protobuf
{
    internal partial class ItemMetadata
    {
        public static ItemMetadata Create(object obj, IReadOnlyDictionary<Type, int> protobufTypeToIndexMap)
        {
            var result = new ItemMetadata();

            if (obj == null || obj is IMessage)
            {
                AddTypeAndSize((IMessage) obj);
            }
            else
            {
                result.TypesAndSizes.Add(-2);
                foreach (var item in (IEnumerable) obj)
                {
                    AddTypeAndSize((IMessage) item);
                }
            }

            return result;

            void AddTypeAndSize(IMessage protobuf)
            {
                if (protobuf == null)
                {
                    result.TypesAndSizes.Add(-1);
                    result.TypesAndSizes.Add(0);
                }
                else
                {
                    var typeInt = protobufTypeToIndexMap[protobuf.GetType()];
                    result.TypesAndSizes.Add(typeInt);
                    result.TypesAndSizes.Add(protobuf.CalculateSize());

                    result._nonNullProtobufs.Add(protobuf);
                }
            }
        }

        private readonly List<IMessage> _nonNullProtobufs = new List<IMessage>();
        public IEnumerable<IMessage> NonNullProtobufs => _nonNullProtobufs;

        public int CalculateTotalSizeBytes()
        {
            switch (TypesAndSizes[0])
            {
                case -2:
                {
                    var sum = 0;
                    for (var i = 2; i < TypesAndSizes.Count; i += 2)
                    {
                        sum += TypesAndSizes[i];
                    }
                    return sum;
                }
                default:
                {
                    return TypesAndSizes[1];
                }
            }
        }

        public object CreateItem(Stream stream, IReadOnlyDictionary<int, Type> protobufIndexToTypeMap)
        {
            switch (TypesAndSizes[0])
            {
                case -2:
                {
                    var result = new List<IMessage>(TypesAndSizes.Count / 2);
                    for (var i = 1; i < TypesAndSizes.Count; i += 2)
                    {
                        var typeIndex = TypesAndSizes[i];
                        var sizeBytes = TypesAndSizes[i + 1];

                        switch (typeIndex)
                        {
                            case -1:
                                result.Add(null);
                                break;
                            default:
                                // Only add the item to the list if it is mapped.
                                // This ensures backwards-compatibility
                                if (protobufIndexToTypeMap.TryGetValue(typeIndex, out var type))
                                {
                                    var protobufModel = (IMessage) Activator.CreateInstance(type);
                                    protobufModel.MergeFrom(stream, sizeBytes);
                                    result.Add(protobufModel);
                                }

                                break;
                        }
                    }
                    return result;
                }
                default:
                {
                    var typeIndex = TypesAndSizes[0];
                    var sizeBytes = TypesAndSizes[1];

                    switch (typeIndex)
                    {
                        case -1:
                            return null;
                        default:
                            var protobufModel = (IMessage) Activator.CreateInstance(protobufIndexToTypeMap[typeIndex]);
                            protobufModel.MergeFrom(stream, sizeBytes);
                            return protobufModel;
                    }
                }
            }
        }
    }

    internal partial class NullableString
    {
        public static implicit operator string(NullableString nullableProto)
        {
            return nullableProto?.Value;
        }

        public static implicit operator NullableString(string value)
        {
            return value == null
                ? null
                : new NullableString { Value = value };
        }
    }
}
