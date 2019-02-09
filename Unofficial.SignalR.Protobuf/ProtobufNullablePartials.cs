using System;
using System.Collections.Generic;
using System.Text;

namespace Unofficial.SignalR.Protobuf
{
    public partial class NullableString
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
