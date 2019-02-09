using System.Diagnostics;
using System.Threading.Tasks;
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
                new TestMessage { Value = "This came from the server!" }
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
