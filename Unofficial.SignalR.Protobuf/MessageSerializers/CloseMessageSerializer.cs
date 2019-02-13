using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public class CloseMessageSerializer : BaseMessageSerializer
    {
        public override ProtobufMessageType EnumType => ProtobufMessageType.Close;
        public override Type MessageType => typeof(CloseMessage);

        protected override IEnumerable<IMessage> CreateProtobufModels(HubMessage message)
        {
            var closeMessage = (CloseMessage) message;

            yield return new CloseMessageProtobuf
            {
                Error = closeMessage.Error
            };
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<IMessage> protobufModels)
        {
            var protobuf = (CloseMessageProtobuf) protobufModels.Single();
            return new CloseMessage(protobuf.Error);
        }
    }
}
