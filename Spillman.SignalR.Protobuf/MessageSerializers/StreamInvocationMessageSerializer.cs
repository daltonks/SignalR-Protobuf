using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Spillman.SignalR.Protobuf.MessageSerializers.Base;
using Spillman.SignalR.Protobuf.Util;
using Unofficial.SignalR.Protobuf;

namespace Spillman.SignalR.Protobuf.MessageSerializers
{
    internal class StreamInvocationMessageSerializer : BaseMessageSerializer
    {
        public override HubMessageType HubMessageType => HubMessageType.StreamInvocation;
        public override Type MessageType => typeof(StreamInvocationMessage);

        protected override IEnumerable<object> CreateItems(HubMessage message)
        {
            var invocationMessage = (StreamInvocationMessage) message;

            yield return new StreamInvocationMessageProtobuf
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
            var protobuf = (StreamInvocationMessageProtobuf) items.First();
            var argumentProtobufs = items.Skip(1).ToArray();

            return new StreamInvocationMessage(
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
