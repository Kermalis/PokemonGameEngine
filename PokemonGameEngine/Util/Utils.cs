using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using Kermalis.PokemonGameEngine.UI;
using System;
using System.IO;
using System.Reflection;

namespace Kermalis.PokemonGameEngine.Util
{
    internal static class Utils
    {
        private const string AssemblyPrefix = "Kermalis.PokemonGameEngine.Assets.";
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        public static Stream GetResourceStream(string resource)
        {
            return _assembly.GetManifestResourceStream(AssemblyPrefix + resource);
        }

        public static string WorkingDirectory { get; private set; }
        public static void SetWorkingDirectory(string workingDirectory)
        {
            PBEDataProvider.InitEngine(workingDirectory, dataProvider: new BattleEngineDataProvider());
            WorkingDirectory = workingDirectory;
        }

        public static double GetAnimationProgress(TimeSpan end, ref TimeSpan cur)
        {
            cur += Program.RenderTimeSinceLastFrame;
            if (cur >= end)
            {
                return 1;
            }
            double p = (double)((decimal)(cur - end).Ticks / end.Ticks) + 1;
            return p;
        }

        public static bool HasShinyCharm()
        {
            return Game.Instance.Save.PlayerInventory[ItemPouchType.KeyItems][ItemType.ShinyCharm] != null;
        }
        public static bool GetRandomShiny()
        {
            return PBEDataProvider.GlobalRandom.RandomBool(HasShinyCharm() ? 3 : 1, 8192);
        }
    }
}
