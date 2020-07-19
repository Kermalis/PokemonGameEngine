using Avalonia;
using Avalonia.Platform;
using Kermalis.PokemonBattleEngine.Utils;
using System.IO;
using System.Reflection;

namespace Kermalis.PokemonGameEngine.Util
{
    internal static class Utils
    {
        private const string AssemblyPrefix = "Kermalis.PokemonGameEngine.Assets.";
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static IPlatformRenderInterface _renderInterface = null;
        public static IPlatformRenderInterface RenderInterface
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
        public static Stream GetResourceStream(string resource)
        {
            return _assembly.GetManifestResourceStream(AssemblyPrefix + resource);
        }

        public static string WorkingDirectory { get; private set; }
        public static void SetWorkingDirectory(string workingDirectory)
        {
            PBEUtils.InitEngine(workingDirectory);
            WorkingDirectory = workingDirectory;
        }
    }
}
