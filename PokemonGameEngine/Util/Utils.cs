using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Item;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetProgress(TimeSpan end, TimeSpan cur)
        {
            if (cur >= end)
            {
                return 1;
            }
            return ((cur - end) / end) + 1;
        }

        public static bool HasShinyCharm()
        {
            return Game.Instance.Save.PlayerInventory[ItemPouchType.KeyItems][ItemType.ShinyCharm] != null;
        }
        public static bool GetRandomShiny()
        {
            return PBEDataProvider.GlobalRandom.RandomBool(HasShinyCharm() ? 3 : 1, 8192);
        }

        public static string GetPkmnDirectoryName(PBESpecies species, PBEForm form)
        {
            string dir;
            if (form == 0)
            {
                dir = species.ToString();
            }
            else
            {
                dir = PBEDataUtils.GetNameOfForm(species, form);
            }
            return dir;
        }

        public static float DegreesToRadiansF(float degrees)
        {
            return MathF.PI / 180 * degrees;
        }
        public static float RadiansToDegreesF(float radians)
        {
            return 180 / MathF.PI * radians;
        }
        public static double DegreesToRadians(double degrees)
        {
            return Math.PI / 180 * degrees;
        }
        public static double RadiansToDegrees(double radians)
        {
            return 180 / Math.PI * radians;
        }
    }
}
