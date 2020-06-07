using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;
using Spillman.SignalR.Protobuf.MessageSerializers.Base;
using Spillman.SignalR.Protobuf.Util;
using Unofficial.SignalR.Protobuf;

namespace Spillman.SignalR.Protobuf.MessageSerializers
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
            var protobuf = (CompletionMessageProtobuf) items[0];
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
