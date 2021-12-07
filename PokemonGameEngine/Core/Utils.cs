using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Item;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Core
{
    internal static class Utils
    {
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
        public static float GetYawRadiansF(this in Quaternion q)
        {
            return MathF.Atan2((2 * q.Y * q.W) - (2 * q.X * q.Z), 1 - (2 * q.Y * q.Y) - (2 * q.Z * q.Z));
        }
        public static float GetPitchRadiansF(this in Quaternion q)
        {
            return MathF.Atan2((2 * q.X * q.W) - (2 * q.Y * q.Z), 1 - (2 * q.X * q.X) - (2 * q.Z * q.Z));
        }
        public static float GetRollRadiansF(this in Quaternion q)
        {
            return MathF.Asin((2 * q.X * q.Y) + (2 * q.Z * q.W));
        }
    }
}
