using GloomServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GloomServer
{
    public class Registrations
    {

        public static void Register(IServiceCollection services)
        {
            RegisterConfigurations(services);
            RegisterServices(services);
        }
        private static void RegisterConfigurations(IServiceCollection services)
        {
            services.RegisterConfiguration<ServerConfiguration>("server.config.json");
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(PathManager));

        }
    }
}
