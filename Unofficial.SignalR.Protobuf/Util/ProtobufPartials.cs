using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using Unofficial.SignalR.Protobuf.Util;

// ReSharper disable once CheckNamespace
namespace Unofficial.SignalR.Protobuf
{
    internal partial class ItemMetadata
    {
        public static ItemMetadata Create(object obj, IReadOnlyDictionary<Type, short> protobufTypeToIndexMap)
        {
            var result = new ItemMetadata();

            switch (obj)
            {
                case IEnumerable<IMessage> items:
                {
                    result.TypesAndSizes.Add(-2);
                    foreach (var item in items)
                    {
                        AddTypeAndSize(item);
                    }
                    break;
                }
                default:
                {
                    AddTypeAndSize(obj as IMessage);
                    break;
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

        public object CreateItem(Stream stream, IReadOnlyList<Type> protobufTypes)
        {
            switch (TypesAndSizes[0])
            {
                case -2:
                {
                    var result = new List<IMessage>(TypesAndSizes.Count / 2);
                    for (var i = 1; i < TypesAndSizes.Count; i += 2)
                    {
                        result.Add(CreateSingular(i));
                    }
                    return result;
                }
                default:
                {
                    return CreateSingular(0);
                }
            }

            IMessage CreateSingular(int typesAndSizesIndex)
            {
                var typeIndex = TypesAndSizes[typesAndSizesIndex];
                var sizeBytes = TypesAndSizes[typesAndSizesIndex + 1];

                switch (typeIndex)
                {
                    case -1:
                        return null;
                    default:
                        var protobufModel = (IMessage) Activator.CreateInstance(protobufTypes[typeIndex]);
                        protobufModel.MergeFrom(stream, sizeBytes);
                        return protobufModel;
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
