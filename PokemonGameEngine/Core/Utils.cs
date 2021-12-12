using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Core
{
    internal static class Utils
    {
        public const float DegToRad = MathF.PI / 180f;
        public const float RadToDeg = 180f / MathF.PI;

        public static float GetYawRadians(this in Quaternion q)
        {
            return MathF.Atan2((2 * q.Y * q.W) - (2 * q.X * q.Z), 1 - (2 * q.Y * q.Y) - (2 * q.Z * q.Z));
        }
        public static float GetPitchRadians(this in Quaternion q)
        {
            return MathF.Atan2((2 * q.X * q.W) - (2 * q.Y * q.Z), 1 - (2 * q.X * q.X) - (2 * q.Z * q.Z));
        }
        public static float GetRollRadians(this in Quaternion q)
        {
            return MathF.Asin((2 * q.X * q.Y) + (2 * q.Z * q.W));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float from, float to, float progress)
        {
            return from + ((to - from) * progress);
        }

        public static bool GetRandomShiny()
        {
            return PBEDataProvider.GlobalRandom.RandomBool(Game.Instance.Save.PlayerInventory.HasShinyCharm() ? 3 : 1, 8192);
        }
    }
}
