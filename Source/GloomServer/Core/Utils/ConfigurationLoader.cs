using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;

namespace GloomServer
{
    public static class ConfigurationLoader
    {
        private static PathManager PathManager { get; set; } = new PathManager(null);

        public static void RegisterConfiguration<T>(this IServiceCollection self, string fileName)
        {
            self.AddSingleton(typeof(T), LoadConfiguration<T>(fileName));
        }

        public static T LoadConfiguration<T>(string fileName)
        {
            string file = Path.Combine(PathManager.ConfigurationDirectory, fileName);

            if (!File.Exists(file))
                WriteConfiguration<T>(file);

            T result = JsonConvert.DeserializeObject<T>(File.ReadAllText(file));

            return result;
        }

        private static void WriteConfiguration<T>(string file, T configration = default)
        {
            if (configration is null || configration.Equals(default))
                File.WriteAllText(file, JsonConvert.SerializeObject(Activator.CreateInstance(typeof(T)), Formatting.Indented));
            else
                File.WriteAllText(file, JsonConvert.SerializeObject(configration, Formatting.Indented));
        }

    }
}
