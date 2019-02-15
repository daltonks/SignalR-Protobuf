using System;
using System.Collections.Generic;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    internal class PingMessageSerializer : BaseMessageSerializer
    {
        public override ProtobufMessageType EnumType => ProtobufMessageType.Ping;
        public override Type MessageType => typeof(PingMessage);

        protected override IEnumerable<IMessage> CreateProtobufModels(HubMessage message)
        {
            yield break;
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<IMessage> protobufModels)
        {
            return PingMessage.Instance;
        }
    }
}
