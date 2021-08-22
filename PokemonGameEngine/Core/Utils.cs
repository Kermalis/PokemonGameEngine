using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Item;
using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Core
{
    internal static class Utils
    {
        private const string AssemblyPrefix = "Kermalis.PokemonGameEngine.Assets.";
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        public static Stream GetResourceStream(string resource)
        {
            Stream s = _assembly.GetManifestResourceStream(AssemblyPrefix + resource);
            if (s is null)
            {
                throw new ArgumentOutOfRangeException(nameof(resource), "Resource not found: " + resource);
            }
            return s;
        }

        public static string GetResourcePathWithoutFilename(string resourceWithFilename)
        {
            bool foundFileExtensionDot = false;
            int len = resourceWithFilename.Length;
            for (int i = resourceWithFilename.Length - 1; i >= 0; i--)
            {
                char c = resourceWithFilename[i];
                if (c == '.')
                {
                    if (foundFileExtensionDot)
                    {
                        len--;
                        break;
                    }
                    else
                    {
                        foundFileExtensionDot = true;
                        len--;
                        continue;
                    }
                }
                len--;
            }
            return resourceWithFilename.Substring(0, len);
        }
        public static string CombineResourcePath(string path, string pathb)
        {
            if (path.Length == 0)
            {
                return pathb;
            }
            return path + '.' + pathb;
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
            return Engine.Instance.Save.PlayerInventory[ItemPouchType.KeyItems][ItemType.ShinyCharm] is not null;
        }
        public static bool GetRandomShiny()
        {
            return PBEDataProvider.GlobalRandom.RandomBool(HasShinyCharm() ? 3 : 1, 8192);
        }

        public static string GetPkmnDirectoryName(PBESpecies species, PBEForm form)
        {
            return form == 0 ? species.ToString() : PBEDataUtils.GetNameOfForm(species, form);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DegreesToRadiansF(float degrees)
        {
            return MathF.PI / 180 * degrees;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RadiansToDegreesF(float radians)
        {
            return 180 / MathF.PI * radians;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DegreesToRadians(double degrees)
        {
            return Math.PI / 180 * degrees;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RadiansToDegrees(double radians)
        {
            return 180 / Math.PI * radians;
        }
        public static float GetYawRadiansF(this Quaternion q)
        {
            return MathF.Atan2((2 * q.Y * q.W) - (2 * q.X * q.Z), 1 - (2 * q.Y * q.Y) - (2 * q.Z * q.Z));
        }
        public static float GetPitchRadiansF(this Quaternion q)
        {
            return MathF.Atan2((2 * q.X * q.W) - (2 * q.Y * q.Z), 1 - (2 * q.X * q.X) - (2 * q.Z * q.Z));
        }
        public static float GetRollRadiansF(this Quaternion q)
        {
            return MathF.Asin((2 * q.X * q.Y) + (2 * q.Z * q.W));
        }
    }
}
