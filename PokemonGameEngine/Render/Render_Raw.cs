using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal delegate uint PixelSupplier(int x, int y);

    internal static unsafe partial class Renderer
    {
        // Colors must be RGBA8888 (0xAABBCCDD - AA is A, BB is B, CC is G, DD is R)

        #region Raw Drawing

        public static void ModulatePoint_Unchecked(uint* dst, float rMod, float gMod, float bMod, float aMod)
        {
            uint current = *dst;
            uint r = GetR(current);
            uint g = GetG(current);
            uint b = GetB(current);
            uint a = GetA(current);
            r = (byte)(r * rMod);
            g = (byte)(g * gMod);
            b = (byte)(b * bMod);
            a = (byte)(a * aMod);
            *dst = Color(r, g, b, a);
        }
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
            *dst = Color(r, g, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawPoint_Checked(uint* dst, int dstW, int dstH, int x, int y, uint color)
        {
            if (y >= 0 && y < dstH && x >= 0 && x < dstW)
            {
                DrawPoint_Unchecked(GetPixelAddress(dst, dstW, x, y), color);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OverwritePoint_Checked(uint* dst, int dstW, int dstH, int x, int y, uint color)
        {
            if (y >= 0 && y < dstH && x >= 0 && x < dstW)
            {
                *GetPixelAddress(dst, dstW, x, y) = color;
            }
        }

        public static uint[] GetBitmap_Unchecked(uint* src, int srcW, int x, int y, int width, int height)
        {
            uint[] arr = new uint[width * height];
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    arr[px + (py * width)] = *GetPixelAddress(src, srcW, x + px, y + py);
                }
            }
            return arr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PixelSupplier MakeBitmapSupplier(uint* src, int srcW)
        {
            return (x, y) => *GetPixelAddress(src, srcW, x, y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint* GetPixelAddress(uint* src, int srcW, int x, int y)
        {
            return src + x + (y * srcW);
        }

        #endregion

        #region Colors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Color(uint r, uint g, uint b, uint a)
        {
            return (a << 24) | (b << 16) | (g << 8) | r;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ColorNoA(uint r, uint g, uint b)
        {
            return (b << 16) | (g << 8) | r;
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
            return Color(newR, g, b, a);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetG(uint color, uint newG)
        {
            uint r = GetR(color);
            uint b = GetB(color);
            uint a = GetA(color);
            return Color(r, newG, b, a);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetB(uint color, uint newB)
        {
            uint r = GetR(color);
            uint g = GetG(color);
            uint a = GetA(color);
            return Color(r, g, newB, a);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetA(uint color, uint newA)
        {
            uint r = GetR(color);
            uint g = GetG(color);
            uint b = GetB(color);
            return Color(r, g, b, newA);
        }

        #endregion
    }
}
