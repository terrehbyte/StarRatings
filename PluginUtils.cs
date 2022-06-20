using System.IO;
using System.Reflection;

namespace StarRatings
{
    public static class PluginUtils
    {
        private static readonly string assemblyLocation;

        public static string InstallationPath => assemblyLocation;

        public static string ResolveIconPath(string installRelativeIconPath) =>
            Path.Combine(InstallationPath, installRelativeIconPath);

        static PluginUtils()
        {
            assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}

// Loading icons from installation path: https://playnite.link/docs/tutorials/extensions/ui.html#example