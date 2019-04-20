using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceDescriptor = Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Unofficial.SignalR.Protobuf
{
    public static class SignalRBuilderExtensions
    {
        public static TBuilder AddProtobufProtocol<TBuilder>(
            this TBuilder builder, 
            IEnumerable<Type> messageTypes
        ) where TBuilder : ISignalRBuilder
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol>(new ProtobufProtocol(messageTypes)));
            return builder;
        }
    }
}
