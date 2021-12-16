using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        // Colors must be RGBA8888 (0xAABBCCDD - AA is A, BB is B, CC is G, DD is R)

        #region Raw Drawing

        public static void DrawPoint_Unchecked(uint* dst, uint color)
        {
            uint aIn = GetA(color);
            if (aIn == 0)
            {
                return; // Fully transparent
            }
            if (aIn == 0xFF)
            {
                *dst = color; // Fully opaque
                return;
            }
            uint rIn = GetR(color);
            uint gIn = GetG(color);
            uint bIn = GetB(color);
            uint current = *dst;
            uint rOld = GetR(current);
            uint gOld = GetG(current);
            uint bOld = GetB(current);
            uint aOld = GetA(current);
            uint r = (rIn * aIn / 0xFF) + (rOld * aOld * (0xFF - aIn) / (0xFF * 0xFF));
            uint g = (gIn * aIn / 0xFF) + (gOld * aOld * (0xFF - aIn) / (0xFF * 0xFF));
            uint b = (bIn * aIn / 0xFF) + (bOld * aOld * (0xFF - aIn) / (0xFF * 0xFF));
            uint a = aIn + (aOld * (0xFF - aIn) / 0xFF);
            *dst = RawColor(r, g, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPixelIndex(uint srcW, int x, int y)
        {
            return (int)(x + (y * srcW));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint* GetPixelAddress(uint* src, uint srcW, int x, int y)
        {
            return src + GetPixelIndex(srcW, x, y);
        }

        #endregion

        #region Colors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RawColor(uint r, uint g, uint b, uint a)
        {
            return (a << 24) | (b << 16) | (g << 8) | r;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetR(uint color)
        {
            return color & 0xFF;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetG(uint color)
        {
            return (color >> 8) & 0xFF;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetB(uint color)
        {
            return (color >> 16) & 0xFF;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetA(uint color)
        {
            return color >> 24;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetR(uint color, uint newR)
        {
            uint g = GetG(color);
            uint b = GetB(color);
            uint a = GetA(color);
            return RawColor(newR, g, b, a);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetG(uint color, uint newG)
        {
            uint r = GetR(color);
            uint b = GetB(color);
            uint a = GetA(color);
            return RawColor(r, newG, b, a);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetB(uint color, uint newB)
        {
            uint r = GetR(color);
            uint g = GetG(color);
            uint a = GetA(color);
            return RawColor(r, g, newB, a);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetA(uint color, uint newA)
        {
            uint r = GetR(color);
            uint g = GetG(color);
            uint b = GetB(color);
            return RawColor(r, g, b, newA);
        }

        #endregion
    }
}
