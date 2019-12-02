using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;
using Unofficial.SignalR.Protobuf.Util;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    internal class StreamItemMessageSerializer : BaseMessageSerializer
    {
        public override HubMessageType HubMessageType => HubMessageType.StreamItem;
        public override Type MessageType => typeof(StreamItemMessage);

        protected override IEnumerable<object> CreateItems(HubMessage message)
        {
            var streamItemMessage = (StreamItemMessage) message;

            yield return new StreamItemMessageProtobuf
            {
                Headers = { streamItemMessage.Headers.Flatten() },
                InvocationId = streamItemMessage.InvocationId
            };

            yield return streamItemMessage.Item;
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items)
        {
            var protobuf = (StreamItemMessageProtobuf) items.First();
            var itemProtobuf = items[1];

            return new StreamItemMessage(protobuf.InvocationId, itemProtobuf)
            {
                Headers = protobuf.Headers.Unflatten()
            };
        }
    }
}
