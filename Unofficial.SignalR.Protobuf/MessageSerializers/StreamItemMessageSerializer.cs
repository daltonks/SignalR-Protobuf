using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    internal class StreamItemMessageSerializer : BaseMessageSerializer
    {
        public override ProtobufMessageType EnumType => ProtobufMessageType.StreamItem;
        public override Type MessageType => typeof(StreamItemMessage);

        protected override IEnumerable<IMessage> CreateProtobufModels(HubMessage message)
        {
            var streamItemMessage = (StreamItemMessage) message;

            yield return new StreamItemMessageProtobuf
            {
                InvocationId = streamItemMessage.InvocationId,
                Headers = { streamItemMessage.Headers.Flatten() }
            };

            yield return (IMessage) streamItemMessage.Item;
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<IMessage> protobufModels)
        {
            var protobuf = (StreamItemMessageProtobuf) protobufModels.First();
            var itemProtobuf = protobufModels[1];

            return new StreamItemMessage(protobuf.InvocationId, itemProtobuf)
            {
                Headers = protobuf.Headers.Unflatten()
            };
        }
    }
}
