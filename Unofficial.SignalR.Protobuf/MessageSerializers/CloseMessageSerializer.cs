using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;
using Unofficial.SignalR.Protobuf.Util;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    internal class CloseMessageSerializer : BaseMessageSerializer
    {
        public override ProtobufMessageType EnumType => ProtobufMessageType.Close;
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
