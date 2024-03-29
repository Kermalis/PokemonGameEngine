﻿using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static class Colors
    {
        public static Vector4 Transparent { get; } = new Vector4(0, 0, 0, 0);
        public static Vector3 Black3 { get; } = new Vector3(0, 0, 0);
        public static Vector4 Black4 { get; } = new Vector4(0, 0, 0, 1);
        public static Vector3 White3 { get; } = new Vector3(1, 1, 1);
        public static Vector4 White4 { get; } = new Vector4(1, 1, 1, 1);
        public static Vector4 Red4 { get; } = new Vector4(1, 0, 0, 1);
        public static Vector4 Green4 { get; } = new Vector4(0, 1, 0, 1);
        public static Vector4 Blue4 { get; } = new Vector4(0, 0, 1, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FromRGB(uint r, uint g, uint b)
        {
            return new Vector3(r / 255f, g / 255f, b / 255f);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 V4FromRGB(uint r, uint g, uint b)
        {
            return new Vector4(r / 255f, g / 255f, b / 255f, 1f);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 FromRGBA(in Vector3 rgb, uint a)
        {
            return new Vector4(rgb, a / 255f);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 FromRGBA(uint r, uint g, uint b, uint a)
        {
            return new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void PutInShader(GL gl, int loc, Vector3 c)
        {
            gl.Uniform3(loc, 1, (float*)&c);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void PutInShader(GL gl, int loc, Vector4 c)
        {
            gl.Uniform4(loc, 1, (float*)&c);
        }
    }
}
