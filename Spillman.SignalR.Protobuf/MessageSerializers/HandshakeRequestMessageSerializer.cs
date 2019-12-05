using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Spillman.SignalR.Protobuf.MessageSerializers.Base;
using Unofficial.SignalR.Protobuf;

namespace Spillman.SignalR.Protobuf.MessageSerializers
{
    internal class HandshakeRequestMessageSerializer : BaseMessageSerializer
    {
        public override HubMessageType HubMessageType => HubMessageType.HandshakeRequest;
        public override Type MessageType => typeof(HandshakeRequestMessage);

        protected override IEnumerable<object> CreateItems(HubMessage message)
        {
            var handshakeRequestMessage = (HandshakeRequestMessage) message;
            yield return new HandshakeRequestMessageProtobuf
            {
                Protocol = handshakeRequestMessage.Protocol,
                Version = handshakeRequestMessage.Version
            };
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items, Exception bindingException)
        {
            var protobuf = (HandshakeRequestMessageProtobuf) items.Single();
            return new HandshakeRequestMessage(protobuf.Protocol, protobuf.Version);
        }
    }
}
