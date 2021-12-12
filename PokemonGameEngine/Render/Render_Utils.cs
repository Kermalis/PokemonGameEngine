﻿using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TextureUnit ToTextureUnit(this int unit)
        {
            return (TextureUnit)((int)TextureUnit.Texture0 + unit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AbsXToRelX(float x)
        {
            return x / GLHelper.CurrentWidth;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AbsXToRelX(float x, uint totalWidth)
        {
            return x / totalWidth;
        }
        /// <summary>0 -> -1, 1 -> 1</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RelXToGLX(float x)
        {
            return (x * 2) - 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RelXToAbsX(float x)
        {
            return (int)(x * GLHelper.CurrentWidth);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AbsYToRelY(float y)
        {
            return y / GLHelper.CurrentHeight;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AbsYToRelY(float y, uint totalHeight)
        {
            return y / totalHeight;
        }
        /// <summary>0 -> 1, 1 -> -1</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RelYToGLY(float y)
        {
            return (y * -2) + 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RelYToAbsY(float y)
        {
            return (int)(y * GLHelper.CurrentHeight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AbsToGL(float x, float y)
        {
            return new Vector2(RelXToGLX(AbsXToRelX(x)), RelYToGLY(AbsYToRelY(y)));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RelToGL(float x, float y)
        {
            return new Vector2(RelXToGLX(x), RelYToGLY(y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint PowerOfTwoize(uint value)
        {
            uint i = 2;
            while (value > i)
            {
                i *= 2;
            }
            return i;
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
    }
}
