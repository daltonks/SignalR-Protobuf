using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;
using Unofficial.SignalR.Protobuf.Util;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    internal class StreamInvocationMessageSerializer : BaseMessageSerializer
    {
        public override ProtobufMessageType EnumType => ProtobufMessageType.StreamInvocation;
        public override Type MessageType => typeof(StreamInvocationMessage);

        protected override IEnumerable<object> CreateItems(HubMessage message)
        {
            var invocationMessage = (StreamInvocationMessage) message;

            yield return new StreamInvocationMessageProtobuf
            {
                InvocationId = invocationMessage.InvocationId,
                Target = invocationMessage.Target,
                Headers = { invocationMessage.Headers.Flatten() }
            };

            foreach (var argument in invocationMessage.Arguments)
            {
                yield return argument;
            }
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items)
        {
            var protobuf = (StreamInvocationMessageProtobuf) items.First();
            var argumentProtobufs = items.Skip(1).ToArray();

            return new StreamInvocationMessage(
                protobuf.InvocationId, 
                protobuf.Target, 
                argumentProtobufs
            )
            {
                Headers = protobuf.Headers.Unflatten()
            };
        }
    }
}
