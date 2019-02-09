using System;
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

            await client.StartAsync();

            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}
