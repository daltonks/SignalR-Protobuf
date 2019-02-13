using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    public class CompletionMessageSerializer : BaseMessageSerializer
    {
        public override ProtobufMessageType EnumType => ProtobufMessageType.Completion;
        public override Type MessageType => typeof(CompletionMessage);

        protected override IEnumerable<IMessage> CreateProtobufModels(HubMessage message)
        {
            var completionMessage = (CompletionMessage) message;

            yield return new CompletionMessageProtobuf
            {
                InvocationId = completionMessage.InvocationId,
                Headers = {completionMessage.Headers.Flatten()},
                Error = completionMessage.Error,
                HasResult = completionMessage.HasResult
            };

            yield return (IMessage) completionMessage.Result;
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<IMessage> protobufModels)
        {
            var protobuf = (CompletionMessageProtobuf) protobufModels.First();
            var resultProtobuf = protobufModels[1];

            return new CompletionMessage(
                protobuf.InvocationId, 
                protobuf.Error, 
                resultProtobuf, 
                protobuf.HasResult
            );
        }
    }
}
