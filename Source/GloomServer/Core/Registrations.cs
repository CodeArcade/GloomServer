using GloomServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GloomServer
{
    public class Registrations
    {
        private static Logger Logger { get; } = LogManager.GetLogger("Registrations");

        public static void Register(IServiceCollection services)
        {
            RegisterConfigurations(services);
            RegisterServices(services);
            RegisterRepositories();
        }
        private static void RegisterConfigurations(IServiceCollection services)
        {
            services.RegisterConfiguration<ServerConfiguration>("server.config.json");
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(PathManager));
        }

        private static void RegisterRepositories()
        {
            RequestHandler.Repositories = new();

            foreach (string dll in GetDlls())
            {
                try
                {
                    foreach (Type t in Assembly.LoadFrom(dll).GetTypes())
                    {
                        if (!typeof(WebSocketRepository).IsAssignableFrom(t)) continue;
                        if (t.IsAbstract) continue;

                        Logger.Debug($"Loading module {t.Name} from {Assembly.LoadFrom(dll).FullName}");

                        RequestHandler.Repositories.Add((WebSocketRepository)Activator.CreateInstance(t));
                    }
                }
                catch (Exception ex) { Logger.Warn(ex); }
            }
        }

        private static List<string> GetDlls() => Directory.GetFiles(new PathManager(null).StartupDirectory).Where(x => x.EndsWith(".dll")).ToList();
    }
}
