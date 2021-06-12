using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        #region Rectangles

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OverwriteRectangle(IImage dstImg, uint color)
        {
            for (int i = 0; i < dstImg.Bitmap.Length; i++)
            {
                dstImg.Bitmap[i] = color;
            }
        }
        public static void OverwriteRectangle(uint* dst, int dstW, int dstH, uint color)
        {
            for (int py = 0; py < dstH; py++)
            {
                for (int px = 0; px < dstW; px++)
                {
                    *GetPixelAddress(dst, dstW, px, py) = color;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OverwriteRectangle(uint* dst, int dstW, int dstH, float x, float y, float width, float height, uint color)
        {
            int ix = (int)(x * dstW);
            int iy = (int)(y * dstH);
            int iw = (int)(width * dstW);
            int ih = (int)(height * dstH);
            OverwriteRectangle(dst, dstW, dstH, ix, iy, iw, ih, color);
        }
        public static void OverwriteRectangle(uint* dst, int dstW, int dstH, int x, int y, int width, int height, uint color)
        {
            if (height == 1)
            {
                if (width == 1)
                {
                    OverwritePoint_Checked(dst, dstW, dstH, x, y, color);
                    return;
                }
                OverwriteHorizontalLine_Width(dst, dstW, dstH, x, y, width, color);
                return;
            }
            if (width == 1)
            {
                OverwriteVerticalLine_Height(dst, dstW, dstH, x, y, height, color);
                return;
            }
            for (int py = y; py < y + height; py++)
            {
                if (py >= 0 && py < dstH)
                {
                    for (int px = x; px < x + width; px++)
                    {
                        if (px >= 0 && px < dstW)
                        {
                            *GetPixelAddress(dst, dstW, px, py) = color;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawRectangle(uint* dst, int dstW, int dstH, uint color)
        {
            DrawRectangle(dst, dstW, dstH, 0, 0, dstW, dstH, color);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawRectangle(uint* dst, int dstW, int dstH, float x, float y, float width, float height, uint color)
        {
            int ix = (int)(x * dstW);
            int iy = (int)(y * dstH);
            int iw = (int)(width * dstW);
            int ih = (int)(height * dstH);
            DrawRectangle(dst, dstW, dstH, ix, iy, iw, ih, color);
        }
        public static void DrawRectangle(uint* dst, int dstW, int dstH, int x, int y, int width, int height, uint color)
        {
            if (height == 1)
            {
                if (width == 1)
                {
                    DrawPoint_Checked(dst, dstW, dstH, x, y, color);
                    return;
                }
                DrawHorizontalLine_Width(dst, dstW, dstH, x, y, width, color);
                return;
            }
            if (width == 1)
            {
                DrawVerticalLine_Height(dst, dstW, dstH, x, y, height, color);
                return;
            }
            // The two vert lines
            DrawVerticalLine_Height(dst, dstW, dstH, x, y, height, color);
            DrawVerticalLine_Height(dst, dstW, dstH, x + width - 1, y, height, color);
            // The two hori lines (don't overlap the vert lines)
            // TODO: This might overlap if the rect is very small
            DrawHorizontalLine_Width(dst, dstW, dstH, x + 1, y, width - 2, color);
            DrawHorizontalLine_Width(dst, dstW, dstH, x + 1, y + height - 1, width - 2, color);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawRectangle_Points(uint* dst, int dstW, int dstH, float x1, float y1, float x2, float y2, uint color)
        {
            int ix1 = (int)(x1 * dstW);
            int iy1 = (int)(y1 * dstH);
            int ix2 = (int)(x2 * dstW);
            int iy2 = (int)(y2 * dstH);
            DrawRectangle_Points(dst, dstW, dstH, ix1, iy1, ix2, iy2, color);
        }
        public static void DrawRectangle_Points(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, uint color)
        {
            if (x1 == x2)
            {
                if (y1 == y2)
                {
                    DrawPoint_Checked(dst, dstW, dstH, x1, y1, color);
                    return;
                }
                DrawVerticalLine_Points(dst, dstW, dstH, x1, y1, y2, color);
                return;
            }
            if (y1 == y2)
            {
                DrawHorizontalLine_Points(dst, dstW, dstH, x1, y1, x2, color);
                return;
            }
            // The two vert lines
            DrawVerticalLine_Points(dst, dstW, dstH, x1, y1, y2, color);
            DrawVerticalLine_Points(dst, dstW, dstH, x2, y1, y2, color);
            // The two hori lines (don't overlap the vert lines)
            // TODO: This might overlap if the rect is very small
            DrawHorizontalLine_Points(dst, dstW, dstH, x1 + 1, y1, x2 - 1, color);
            DrawHorizontalLine_Points(dst, dstW, dstH, x1 + 1, y2, x2 - 1, color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawThickRectangle(uint* dst, int dstW, int dstH, int thickness, uint color)
        {
            DrawThickRectangle(dst, dstW, dstH, 0, 0, dstW, dstH, thickness, color);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawThickRectangle(uint* dst, int dstW, int dstH, float x, float y, float width, float height, int thickness, uint color)
        {
            int ix = (int)(x * dstW);
            int iy = (int)(y * dstH);
            int iw = (int)(width * dstW);
            int ih = (int)(height * dstH);
            DrawThickRectangle(dst, dstW, dstH, ix, iy, iw, ih, thickness, color);
        }
        public static void DrawThickRectangle(uint* dst, int dstW, int dstH, int x, int y, int width, int height, int thickness, uint color)
        {
            if (thickness < 1)
            {
                return;
            }
            for (int i = 0; i < thickness; i++)
            {
                // The vert lines
                DrawVerticalLine_Height(dst, dstW, dstH, x + i, y, height, color);
                DrawVerticalLine_Height(dst, dstW, dstH, x + width - 1 - i, y, height, color);
                // This might overlap if the rect is very small (TODO)
                // The hori lines (don't overlap the vert lines)
                DrawHorizontalLine_Width(dst, dstW, dstH, x + thickness, y + i, width - 1 - thickness, color);
                DrawHorizontalLine_Width(dst, dstW, dstH, x + thickness, y + height - 1 - i, width - 1 - thickness, color);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawThickRectangle_Points(uint* dst, int dstW, int dstH, float x1, float y1, float x2, float y2, int thickness, uint color)
        {
            int ix1 = (int)(x1 * dstW);
            int iy1 = (int)(y1 * dstH);
            int ix2 = (int)(x2 * dstW);
            int iy2 = (int)(y2 * dstH);
            DrawThickRectangle_Points(dst, dstW, dstH, ix1, iy1, ix2, iy2, thickness, color);
        }
        public static void DrawThickRectangle_Points(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, int thickness, uint color)
        {
            if (thickness < 1)
            {
                return;
            }
            for (int i = 0; i < thickness; i++)
            {
                // The vert lines
                DrawVerticalLine_Points(dst, dstW, dstH, x1 + i, y1, y2, color);
                DrawVerticalLine_Points(dst, dstW, dstH, x2 - i, y1, y2, color);
                // This might overlap if the rect is very small (TODO)
                // The hori lines (don't overlap the vert lines)
                DrawHorizontalLine_Points(dst, dstW, dstH, x1 + thickness, y1 + i, x2 - thickness, color);
                DrawHorizontalLine_Points(dst, dstW, dstH, x1 + thickness, y2 - i, x2 - thickness, color);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillRectangle(uint* dst, int dstW, int dstH, uint color)
        {
            FillRectangle(dst, dstW, dstH, 0, 0, dstW, dstH, color);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillRectangle(uint* dst, int dstW, int dstH, float x, float y, float width, float height, uint color)
        {
            int ix = (int)(x * dstW);
            int iy = (int)(y * dstH);
            int iw = (int)(width * dstW);
            int ih = (int)(height * dstH);
            FillRectangle(dst, dstW, dstH, ix, iy, iw, ih, color);
        }
        public static void FillRectangle(uint* dst, int dstW, int dstH, int x, int y, int width, int height, uint color)
        {
            if (height == 1)
            {
                if (width == 1)
                {
                    DrawPoint_Checked(dst, dstW, dstH, x, y, color);
                    return;
                }
                DrawHorizontalLine_Width(dst, dstW, dstH, x, y, width, color);
                return;
            }
            if (width == 1)
            {
                DrawVerticalLine_Height(dst, dstW, dstH, x, y, height, color);
                return;
            }
            for (int py = y; py < y + height; py++)
            {
                if (py >= 0 && py < dstH)
                {
                    for (int px = x; px < x + width; px++)
                    {
                        if (px >= 0 && px < dstW)
                        {
                            DrawPoint_Unchecked(GetPixelAddress(dst, dstW, px, py), color);
                        }
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillRectangle_Points(uint* dst, int dstW, int dstH, float x1, float y1, float x2, float y2, uint color)
        {
            int ix1 = (int)(x1 * dstW);
            int iy1 = (int)(y1 * dstH);
            int ix2 = (int)(x2 * dstW);
            int iy2 = (int)(y2 * dstH);
            FillRectangle_Points(dst, dstW, dstH, ix1, iy1, ix2, iy2, color);
        }
        public static void FillRectangle_Points(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, uint color)
        {
            if (x1 == x2)
            {
                if (y1 == y2)
                {
                    DrawPoint_Checked(dst, dstW, dstH, x1, y1, color);
                    return;
                }
                DrawVerticalLine_Points(dst, dstW, dstH, x1, y1, y2, color);
                return;
            }
            if (y1 == y2)
            {
                DrawHorizontalLine_Points(dst, dstW, dstH, x1, y1, x2, color);
                return;
            }
            for (int py = y1; py <= y2; py++)
            {
                if (py >= 0 && py < dstH)
                {
                    for (int px = x1; px <= x2; px++)
                    {
                        if (px >= 0 && px < dstW)
                        {
                            DrawPoint_Unchecked(GetPixelAddress(dst, dstW, px, py), color);
                        }
                    }
                }
            }
        }
        public static void ModulateRectangle(uint* dst, int dstW, int dstH, float rMod, float gMod, float bMod, float aMod)
        {
            for (int y = 0; y < dstH; y++)
            {
                for (int x = 0; x < dstW; x++)
                {
                    ModulatePoint_Unchecked(GetPixelAddress(dst, dstW, x, y), rMod, gMod, bMod, aMod);
                }
            }
        }

        #endregion

        #region Rounded Rectangles

        // https://www.freebasic.net/forum/viewtopic.php?t=19874

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawRoundedRectangle(uint* dst, int dstW, int dstH, float x1, float y1, float x2, float y2, int radius, uint color)
        {
            int ix1 = (int)(x1 * dstW);
            int iy1 = (int)(y1 * dstH);
            int ix2 = (int)(x2 * dstW);
            int iy2 = (int)(y2 * dstH);
            DrawRoundedRectangle(dst, dstW, dstH, ix1, iy1, ix2, iy2, radius, color);
        }
        public static void DrawRoundedRectangle(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, int radius, uint color)
        {
            int f = 1 - radius;
            int ddF_x = 1;
            int ddF_y = -2 * radius;
            int xx = 0;
            int yy = radius;
            while (xx < yy)
            {
                if (f >= 0)
                {
                    yy -= 1;
                    ddF_y += 2;
                    f += ddF_y;
                }
                xx += 1;
                ddF_x += 2;
                f += ddF_x;
                int t1 = y1 - yy + radius;
                int t2 = y1 - xx + radius;
                int l1 = x1 - xx + radius;
                int l2 = x1 - yy + radius;
                int b1 = y2 + yy - radius;
                int b2 = y2 + xx - radius;
                int r1 = x2 + xx - radius;
                int r2 = x2 + yy - radius;
                DrawPoint_Checked(dst, dstW, dstH, l1, t1, color);
                DrawPoint_Checked(dst, dstW, dstH, r1, t1, color);
                if (t1 != t2)
                {
                    DrawPoint_Checked(dst, dstW, dstH, l2, t2, color);
                    DrawPoint_Checked(dst, dstW, dstH, r2, t2, color);
                }
                DrawPoint_Checked(dst, dstW, dstH, l1, b1, color);
                DrawPoint_Checked(dst, dstW, dstH, r1, b1, color);
                if (b1 != b2)
                {
                    DrawPoint_Checked(dst, dstW, dstH, l2, b2, color);
                    DrawPoint_Checked(dst, dstW, dstH, r2, b2, color);
                }
            }
            DrawHorizontalLine_Points(dst, dstW, dstH, x1 + radius, y1, x2 - radius, color); // Top
            DrawHorizontalLine_Points(dst, dstW, dstH, x1 + radius, y2, x2 - radius, color); // Bottom
            DrawVerticalLine_Points(dst, dstW, dstH, x1, y1 + radius, y2 - radius, color); // Left
            DrawVerticalLine_Points(dst, dstW, dstH, x2, y1 + radius, y2 - radius, color); // Right
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillRoundedRectangle(uint* dst, int dstW, int dstH, float x1, float y1, float x2, float y2, int radius, uint color)
        {
            int ix1 = (int)(x1 * dstW);
            int iy1 = (int)(y1 * dstH);
            int ix2 = (int)(x2 * dstW);
            int iy2 = (int)(y2 * dstH);
            FillRoundedRectangle(dst, dstW, dstH, ix1, iy1, ix2, iy2, radius, color);
        }
        public static void FillRoundedRectangle(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, int radius, uint color)
        {
            int f = 1 - radius;
            int ddF_x = 1;
            int ddF_y = -2 * radius;
            int xx = 0;
            int yy = radius;
            int prevT1 = -1;
            int prevB1 = -1;
            while (xx < yy)
            {
                if (f >= 0)
                {
                    yy -= 1;
                    ddF_y += 2;
                    f += ddF_y;
                }
                xx += 1;
                ddF_x += 2;
                f += ddF_x;
                int r1 = x2 + xx - radius;
                int r2 = x2 + yy - radius;
                int l1 = x1 - xx + radius;
                int l2 = x1 - yy + radius;
                int b1 = y2 + yy - radius;
                int b2 = y2 + xx - radius;
                int t1 = y1 - yy + radius;
                int t2 = y1 - xx + radius;
                if (t1 == prevT1)
                {
                    DrawPoint_Checked(dst, dstW, dstH, l1, t1, color);
                    DrawPoint_Checked(dst, dstW, dstH, r1, t1, color);
                }
                else
                {
                    DrawHorizontalLine_Points(dst, dstW, dstH, l1, t1, r1, color);
                    prevT1 = t1;
                }
                if (t1 != t2)
                {
                    DrawHorizontalLine_Points(dst, dstW, dstH, l2, t2, r2, color);
                }
                if (b1 == prevB1)
                {
                    DrawPoint_Checked(dst, dstW, dstH, l1, b1, color);
                    DrawPoint_Checked(dst, dstW, dstH, r1, b1, color);
                }
                else
                {
                    DrawHorizontalLine_Points(dst, dstW, dstH, l1, b1, r1, color);
                    prevB1 = b1;
                }
                if (b1 != b2)
                {
                    DrawHorizontalLine_Points(dst, dstW, dstH, l2, b2, r2, color);
                }
            }
            FillRectangle_Points(dst, dstW, dstH, x1, y1 + radius, x2, y2 - radius, color); // Middle
        }

        #endregion
    }
}
