using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Spillman.SignalR.Protobuf.MessageSerializers.Base;
using Spillman.SignalR.Protobuf.Util;
using Unofficial.SignalR.Protobuf;

namespace Spillman.SignalR.Protobuf.MessageSerializers
{
    internal class CancelInvocationMessageSerializer : BaseMessageSerializer
    {
        public override HubMessageType HubMessageType => HubMessageType.CancelInvocation;
        public override Type MessageType => typeof(CancelInvocationMessage);

        protected override IEnumerable<object> CreateItems(HubMessage message)
        {
            var cancelInvocationMessage = (CancelInvocationMessage) message;

            yield return new CancelInvocationMessageProtobuf
            {
                InvocationId = cancelInvocationMessage.InvocationId,
                Headers = { cancelInvocationMessage.Headers.Flatten() }
            };
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items, Exception bindingException)
        {
            var protobuf = (CancelInvocationMessageProtobuf) items.Single();
            
            return new CancelInvocationMessage(protobuf.InvocationId)
            {
                Headers = protobuf.Headers.Unflatten()
            };
        }
    }
}
