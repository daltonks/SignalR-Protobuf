using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Spillman.SignalR.Protobuf.MessageSerializers.Base;
using Unofficial.SignalR.Protobuf;

namespace Spillman.SignalR.Protobuf.MessageSerializers
{
    internal class CloseMessageSerializer : BaseMessageSerializer
    {
        public override HubMessageType HubMessageType => HubMessageType.Close;
        public override Type MessageType => typeof(CloseMessage);

        protected override IEnumerable<object> CreateItems(HubMessage message)
        {
            var closeMessage = (CloseMessage) message;

            yield return new CloseMessageProtobuf
            {
                Error = closeMessage.Error
            };
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items)
        {
            var protobuf = (CloseMessageProtobuf) items.Single();
            return new CloseMessage(protobuf.Error);
        }
    }
}
