﻿using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Unofficial.SignalR.Protobuf.Test.Core;

namespace Unofficial.SignalR.Protobuf.Test.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            using (var stream = typeof(Startup)
                .Assembly
                .GetManifestResourceStream("Unofficial.SignalR.Protobuf.Test.Server.SignalR Connection String.txt"))
            using (var streamReader = new StreamReader(stream))
            {
                var signalRConnectionString = streamReader.ReadToEnd();

                services
                    .AddSignalR()
                    .AddAzureSignalR(signalRConnectionString)
                    .AddProtobufProtocol(MessagesReflection.Descriptor.MessageTypes);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            
            app.UseAzureSignalR(routes =>
            {
                routes.MapHub<TestHub>("/realtime");
            });

            app.UseMvc();
        }
    }
}
