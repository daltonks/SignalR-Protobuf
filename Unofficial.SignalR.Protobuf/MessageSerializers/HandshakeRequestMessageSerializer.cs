using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public class HandshakeRequestMessageSerializer : BaseMessageSerializer
    {
        public override ProtobufMessageType EnumType => ProtobufMessageType.HandshakeRequest;
        public override Type MessageType => typeof(HandshakeRequestMessage);

        protected override IEnumerable<IMessage> CreateProtobufModels(HubMessage message)
        {
            var handshakeRequestMessage = (HandshakeRequestMessage) message;
            yield return new HandshakeRequestMessageProtobuf
            {
                Protocol = handshakeRequestMessage.Protocol,
                Version = handshakeRequestMessage.Version
            };
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<IMessage> protobufModels)
        {
            var protobuf = (HandshakeRequestMessageProtobuf) protobufModels.Single();
            return new HandshakeRequestMessage(protobuf.Protocol, protobuf.Version);
        }
    }
}
