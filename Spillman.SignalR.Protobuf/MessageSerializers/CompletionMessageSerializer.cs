using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
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

        protected override HubMessage CreateHubMessage(IReadOnlyList<object> items, Exception bindingException)
        {
            var protobuf = (CompletionMessageProtobuf) items.First();

            if (bindingException != null)
            {
                return new InvocationBindingFailureMessage(
                    protobuf.InvocationId,
                    null, 
                    ExceptionDispatchInfo.Capture(bindingException)
                );
            }

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
