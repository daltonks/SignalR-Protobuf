using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR;
using Unofficial.SignalR.Protobuf.Test.Core;

namespace Unofficial.SignalR.Protobuf.Test.Server
{
    public class TestHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            Debug.WriteLine("Client connected!");
            
            await Clients.Caller.SendAsync(
                "HandleTestMessage", 
                new List<IMessage> {
                    new TestMessage { Value = "First message from the server!" },
                    new TestMessage { Value = "Second message from the server!" }
                }
            );

            await base.OnConnectedAsync();
        }

        public Task HandleTestMessage(TestMessage message)
        {
            Debug.WriteLine($"Server received \"{message.Value}\"");

            return Task.CompletedTask;
        }
    }
}
