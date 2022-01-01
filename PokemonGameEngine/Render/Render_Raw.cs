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
        public static int GetPixelIndex(uint srcW, Pos2D pos)
        {
            return pos.X + (pos.Y * (int)srcW);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint* GetPixelAddress(uint* src, uint srcW, Pos2D pos)
        {
            return src + GetPixelIndex(srcW, pos);
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

        #endregion
    }
}
