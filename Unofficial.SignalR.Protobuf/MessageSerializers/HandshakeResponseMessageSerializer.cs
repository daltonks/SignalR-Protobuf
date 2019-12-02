using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    internal class HandshakeResponseMessageSerializer : BaseMessageSerializer
    {
        public override HubMessageType HubMessageType => HubMessageType.HandshakeResponse;
        public override Type MessageType => typeof(HandshakeResponseMessage);

        protected override IEnumerable<object> CreateItems(HubMessage message)
        {
            var handshakeResponseMessage = (HandshakeResponseMessage) message;
            
            yield return new HandshakeResponseMessageProtobuf
            {
                Error = handshakeResponseMessage.Error
            };
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items)
        {
            var protobuf = (HandshakeResponseMessageProtobuf) items.Single();
            return new HandshakeResponseMessage(protobuf.Error);
        }
    }
}
