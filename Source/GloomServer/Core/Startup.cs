using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GloomServer
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // register our custom middleware since we use the IMiddleware factory approach
            services.AddTransient<WebSocketMiddleware>();

            // register the background process to periodically send a timestamp to clients
            services.AddHostedService<BroadcastTimestamp>();

            Registrations.Register(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            System.IO.Directory.SetCurrentDirectory(env.ContentRootPath);

            // enable websocket support
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120)
            });

            // add our custom middleware to the pipeline
            app.UseMiddleware<WebSocketMiddleware>();

            if (!string.IsNullOrEmpty(ConfigurationLoader.LoadConfiguration<ServerConfiguration>("server.config.json").Certificate)) 
                app.UseHttpsRedirection();
        }
    }
}
