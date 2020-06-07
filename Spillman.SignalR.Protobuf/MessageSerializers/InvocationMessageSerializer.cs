using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Spillman.SignalR.Protobuf.MessageSerializers.Base;
using Spillman.SignalR.Protobuf.Util;
using Unofficial.SignalR.Protobuf;

namespace Spillman.SignalR.Protobuf.MessageSerializers
{
    internal class InvocationMessageSerializer : BaseMessageSerializer
    {
        public override HubMessageType HubMessageType => HubMessageType.Invocation;
        public override Type MessageType => typeof(InvocationMessage);

        protected override IEnumerable<object> CreateItems(HubMessage message)
        {
            var invocationMessage = (InvocationMessage) message;

            yield return new InvocationMessageProtobuf
            {
                Headers = { invocationMessage.Headers.Flatten() },
                InvocationId = invocationMessage.InvocationId,
                Target = invocationMessage.Target,
                StreamIds = { invocationMessage.StreamIds ?? Enumerable.Empty<string>() }
            };

            foreach (var argument in invocationMessage.Arguments)
            {
                yield return argument;
            }
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items)
        {
            var protobuf = (InvocationMessageProtobuf) items.First();
            var argumentProtobufs = items.Skip(1).ToArray();

            return new InvocationMessage(
                protobuf.InvocationId, 
                protobuf.Target, 
                argumentProtobufs,
                protobuf.StreamIds.ToArray()
            )
            {
                Headers = protobuf.Headers.Unflatten()
            };
        }
    }
}
