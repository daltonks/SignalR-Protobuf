using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    internal class InvocationMessageSerializer : BaseMessageSerializer
    {
        public override ProtobufMessageType EnumType => ProtobufMessageType.Invocation;
        public override Type MessageType => typeof(InvocationMessage);

        protected override IEnumerable<IMessage> CreateProtobufModels(HubMessage message)
        {
            var invocationMessage = (InvocationMessage) message;

            yield return new InvocationMessageProtobuf
            {
                InvocationId = invocationMessage.InvocationId,
                Target = invocationMessage.Target,
                Headers = { invocationMessage.Headers.Flatten() }
            };

            foreach (var argument in invocationMessage.Arguments.Cast<IMessage>())
            {
                yield return argument;
            }
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<IMessage> protobufModels)
        {
            var protobuf = (InvocationMessageProtobuf) protobufModels.First();
            var argumentProtobufs = protobufModels.Skip(1).Cast<object>().ToArray();

            return new InvocationMessage(
                protobuf.InvocationId, 
                protobuf.Target, 
                argumentProtobufs
            )
            {
                Headers = protobuf.Headers.Unflatten()
            };
        }
    }
}
