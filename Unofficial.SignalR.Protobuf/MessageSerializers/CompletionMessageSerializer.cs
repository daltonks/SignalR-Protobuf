using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Protocol;
using Unofficial.SignalR.Protobuf.MessageSerializers.Base;
using Unofficial.SignalR.Protobuf.Util;

namespace Unofficial.SignalR.Protobuf.MessageSerializers
{
    internal class CompletionMessageSerializer : BaseMessageSerializer
    {
        public override HubMessageType HubMessageType => HubMessageType.Completion;
        public override Type MessageType => typeof(CompletionMessage);

        protected override IEnumerable<object> CreateItems(HubMessage message)
        {
            var completionMessage = (CompletionMessage) message;

            yield return new CompletionMessageProtobuf
            {
                InvocationId = completionMessage.InvocationId,
                Headers = {completionMessage.Headers.Flatten()},
                Error = completionMessage.Error,
                HasResult = completionMessage.HasResult
            };

            yield return completionMessage.Result;
        }

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items)
        {
            var protobuf = (CompletionMessageProtobuf) items.First();
            var resultProtobuf = items[1];

            return new CompletionMessage(
                protobuf.InvocationId, 
                protobuf.Error, 
                resultProtobuf, 
                protobuf.HasResult
            );
        }
    }
}
