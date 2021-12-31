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

        public static Vector4 MulMatrixAndVec4(in Matrix4x4 m, in Vector4 v)
        {
            Vector4 ret;
            ret.X = v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + v.W * m.M41;
            ret.Y = v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + v.W * m.M42;
            ret.Z = v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + v.W * m.M43;
            ret.W = v.X * m.M14 + v.Y * m.M24 + v.Z * m.M34 + v.W * m.M44;
            return ret;
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
