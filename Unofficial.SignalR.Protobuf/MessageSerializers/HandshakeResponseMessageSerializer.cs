using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    internal class HandshakeResponseMessageSerializer : BaseMessageSerializer
    {
        public override ProtobufMessageType EnumType => ProtobufMessageType.HandshakeResponse;
        public override Type MessageType => typeof(HandshakeResponseMessage);

        protected override IEnumerable<IMessage> CreateProtobufModels(HubMessage message)
        {
            var handshakeResponseMessage = (HandshakeResponseMessage) message;

            yield return new HandshakeResponseMessageProtobuf
            {
                Error = handshakeResponseMessage.Error
            };
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<IMessage> protobufModels)
        {
            var protobuf = (HandshakeResponseMessageProtobuf) protobufModels.Single();
            return new HandshakeResponseMessage(protobuf.Error);
        }
    }
}
