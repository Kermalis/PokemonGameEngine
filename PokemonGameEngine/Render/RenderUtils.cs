using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe class RenderUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TextureUnit ToTextureUnit(this int unit)
        {
            return (TextureUnit)((int)TextureUnit.Texture0 + unit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCoordinatesForCentering(uint dstSize, uint srcSize, float pos)
        {
            return (int)((uint)(dstSize * pos) - (srcSize / 2));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCoordinatesForEndAlign(uint dstSize, uint srcSize, float pos)
        {
            return (int)((uint)(dstSize * pos) - srcSize);
        }

        public static void ClearColor(this GL gl, in Vector3 color)
        {
            gl.ClearColor(color.X, color.Y, color.Z, 1f);
        }
        public static void ClearColor(this GL gl, in Vector4 color)
        {
            gl.ClearColor(color.X, color.Y, color.Z, color.W);
        }
    }
}
