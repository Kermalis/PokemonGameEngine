using Avalonia;
using Avalonia.Platform;
using Kermalis.PokemonBattleEngine;
using System.IO;
using System.Reflection;

namespace Kermalis.PokemonGameEngine.Util
{
    public static class Utils
    {
        private const string AssemblyPrefix = "Kermalis.PokemonGameEngine.Assets.";
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static IPlatformRenderInterface _renderInterface = null;
        internal static IPlatformRenderInterface RenderInterface
        {
            get
            {
                // This is done because the static constructor of Utils is called (by SetWorkingDirectory) before the Avalonia app is built
                if (_renderInterface == null)
                {
                    _renderInterface = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
                }
                return _renderInterface;
            }
        }
        internal static Stream GetResourceStream(string resource)
        {
            return _assembly.GetManifestResourceStream(AssemblyPrefix + resource);
        }

        public static string WorkingDirectory { get; private set; }
        public static void SetWorkingDirectory(string workingDirectory)
        {
            PBEUtils.CreateDatabaseConnection(workingDirectory);
            WorkingDirectory = workingDirectory;
        }
    }
}
