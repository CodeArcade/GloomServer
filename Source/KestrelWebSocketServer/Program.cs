using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GloomServer
{
    public class Program
    {
        public const int TIMESTAMP_INTERVAL_SEC = 15;
        public const int BROADCAST_TRANSMIT_INTERVAL_MS = 250;
        public const int CLOSE_SOCKET_TIMEOUT_MS = 2500;

        private static PathManager PathManager { get; set; } = new PathManager(null);
        private static ServerConfiguration ServerConfiguration { get; set; } = ConfigurationLoader.LoadConfiguration<ServerConfiguration>("server.config.json");

        private static Logger Logger { get; set; }

        public static void Main(string[] args)
        {
            SetupLog();

            try
            {
                Logger.Info("Starting Server");
                Logger.Info($"Running with https: {!string.IsNullOrEmpty(ServerConfiguration.Certificate)}");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static void SetupLog()
        {
            string file = Path.Combine(PathManager.ConfigurationDirectory, "nlog.config");
            if (!File.Exists(file))
                File.Copy(Path.Combine(PathManager.StartupDirectory, Path.Combine("Properties", "nlog.config")), file);

            Logger = NLogBuilder.ConfigureNLog(file).GetCurrentClassLogger();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // hack to get rid of double address binding warning
                    // https://stackoverflow.com/questions/51738893/removing-kestrel-binding-warning
                    webBuilder.UseUrls();

                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Parse(ServerConfiguration.Ip), ServerConfiguration.Port, listenOptions =>
                        {
                            if (!string.IsNullOrEmpty(ServerConfiguration.Certificate)) // PFX cert is required
                                listenOptions.UseHttps(ServerConfiguration.Certificate, ServerConfiguration.CertificatePassword);
                        });
                    });

                    webBuilder.ConfigureLogging((hostContext, logging) =>
                    {
                        logging.ClearProviders();
                        logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    });

                    webBuilder.UseNLog();
                });
        }

        public static void ReportException(Exception ex, [CallerMemberName] string location = "(Caller name not set)")
        {
            Logger.Error($"\n{location}:\n  Exception {ex.GetType().Name}: {ex}");
        }
    }
}
