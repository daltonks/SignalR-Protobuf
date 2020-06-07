using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;
using Spillman.SignalR.Protobuf.MessageSerializers.Base;

namespace Spillman.SignalR.Protobuf.MessageSerializers
{
    internal class PingMessageSerializer : BaseMessageSerializer
    {
        public override HubMessageType HubMessageType => HubMessageType.Ping;
        public override Type MessageType => typeof(PingMessage);

        protected override IEnumerable<object> CreateItems(HubMessage message)
        {
            yield break;
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items)
        {
            return PingMessage.Instance;
        }
    }
}
