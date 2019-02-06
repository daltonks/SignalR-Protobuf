using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceDescriptor = Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Unofficial.SignalR.Protobuf
{
    public static class SignalRBuilderExtensions
    {
        public static TBuilder AddProtobufProtocol<TBuilder>(
            this TBuilder builder,
            IEnumerable<MessageDescriptor> messageDescriptors
        ) where TBuilder : ISignalRBuilder
        {
            return builder.AddProtobufProtocol(
                messageDescriptors
                    .Select(messageDescriptor => messageDescriptor.ClrType)
                    .ToList()
            );
        }

        public static TBuilder AddProtobufProtocol<TBuilder>(
            this TBuilder builder, 
            IReadOnlyList<Type> messageTypes
        ) where TBuilder : ISignalRBuilder
        {
            builder.Services.RemoveAll<IHubProtocol>();
            builder.Services.AddSingleton<IHubProtocol>(new ProtobufProtocol(messageTypes));
            return builder;
        }
    }
}
