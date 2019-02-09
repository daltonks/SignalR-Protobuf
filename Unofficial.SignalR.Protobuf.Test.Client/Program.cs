using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Unofficial.SignalR.Protobuf.Test.Core;

namespace Unofficial.SignalR.Protobuf.Test.Client
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var client = new HubConnectionBuilder()
                .WithUrl("http://localhost:51429/realtime")
                .AddProtobufProtocol(MessagesReflection.Descriptor.MessageTypes)
                .Build();

            client.On<TestMessage>(
                "HandleTestMessage",
                message =>
                {
                    Debug.WriteLine($"Client received \"{message.Value}\"");
                }
            );

            await client.StartAsync();
            await client.InvokeAsync(
                "HandleTestMessage", 
                new TestMessage { Value = "This came from the client!" }
            );

            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}
