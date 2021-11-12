using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Reflection;

namespace GloomServer
{
    public class PathManager
    {
        public readonly IWebHostEnvironment Environment;

        public PathManager(IWebHostEnvironment environment)
        {
            Environment = environment;
        }

        public string WebRootDirectory => Environment.WebRootPath;
        public string StartupDirectory => CheckDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        public string AboveStartupDirectory => CheckDirectory(Directory.GetParent(StartupDirectory).FullName);
        public string LogDirectory => CheckDirectory(Path.Combine(AboveStartupDirectory, "Logs"));
        public string ConfigurationDirectory => CheckDirectory(Path.Combine(AboveStartupDirectory, "Configurations"));

        private static string CheckDirectory(string path)
        {
            if (Directory.Exists(path)) return path;
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
