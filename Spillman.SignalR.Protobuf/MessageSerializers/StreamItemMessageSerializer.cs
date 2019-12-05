using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.SignalR.Protocol;
using Spillman.SignalR.Protobuf.MessageSerializers.Base;
using Spillman.SignalR.Protobuf.Util;
using Unofficial.SignalR.Protobuf;

namespace Spillman.SignalR.Protobuf.MessageSerializers
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

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items, Exception bindingException)
        {
            var protobuf = (StreamItemMessageProtobuf) items.First();

            if (bindingException != null)
            {
                return new StreamBindingFailureMessage(
                    protobuf.InvocationId,
                    ExceptionDispatchInfo.Capture(bindingException)
                );
            }

            var itemProtobuf = items[1];

            return new StreamItemMessage(protobuf.InvocationId, itemProtobuf)
            {
                Headers = protobuf.Headers.Unflatten()
            };
        }
    }
}
