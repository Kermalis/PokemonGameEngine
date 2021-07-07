// This file is adapted from SDL
// https://github.com/spurious/SDL-mirror/blob/master/src/render/software/SDL_rotate.c
// https://github.com/spurious/SDL-mirror/blob/master/src/render/software/SDL_render_sw.c
using System;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        #region 90 degree multiples

        public static void OverwriteBitmapRotated90CW(uint* dst, int dstW, int dstH, int x, int y, PixelSupplier src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
        {
            for (int cy = 0; cy < srcW; cy++)
            {
                int py = yFlip ? (y + (srcH - 1 - cy)) : (y + cy);
                if (py >= 0 && py < dstH)
                {
                    for (int cx = 0; cx < srcH; cx++)
                    {
                        int px = xFlip ? (x + (srcW - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < dstW)
                        {
                            *GetPixelAddress(dst, dstW, px, py) = src(cy, srcH - cx - 1);
                        }
                    }
                }
            }
        }
        public static void OverwriteBitmapRotated180CW(uint* dst, int dstW, int dstH, int x, int y, PixelSupplier src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
        {
            for (int cy = 0; cy < srcH; cy++)
            {
                int py = yFlip ? (y + (srcH - 1 - cy)) : (y + cy);
                if (py >= 0 && py < dstH)
                {
                    for (int cx = 0; cx < srcW; cx++)
                    {
                        int px = xFlip ? (x + (srcW - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < dstW)
                        {
                            *GetPixelAddress(dst, dstW, px, py) = src(srcW - cx - 1, srcH - cy - 1);
                        }
                    }
                }
            }
        }
        public static void OverwriteBitmapRotated270CW(uint* dst, int dstW, int dstH, int x, int y, PixelSupplier src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
        {
            for (int cy = 0; cy < srcW; cy++)
            {
                int py = yFlip ? (y + (srcH - 1 - cy)) : (y + cy);
                if (py >= 0 && py < dstH)
                {
                    for (int cx = 0; cx < srcH; cx++)
                    {
                        int px = xFlip ? (x + (srcW - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < dstW)
                        {
                            *GetPixelAddress(dst, dstW, px, py) = src(srcW - cy - 1, cx);
                        }
                    }
                }
            }
        }

        public static void DrawBitmapRotated90CW(uint* dst, int dstW, int dstH, int x, int y, PixelSupplier src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
        {
            for (int cy = 0; cy < srcW; cy++)
            {
                int py = yFlip ? (y + (srcH - 1 - cy)) : (y + cy);
                if (py >= 0 && py < dstH)
                {
                    for (int cx = 0; cx < srcH; cx++)
                    {
                        int px = xFlip ? (x + (srcW - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < dstW)
                        {
                            DrawPoint_Unchecked(GetPixelAddress(dst, dstW, px, py), src(cy, srcH - cx - 1));
                        }
                    }
                }
            }
        }
        public static void DrawBitmapRotated180CW(uint* dst, int dstW, int dstH, int x, int y, PixelSupplier src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
        {
            for (int cy = 0; cy < srcH; cy++)
            {
                int py = yFlip ? (y + (srcH - 1 - cy)) : (y + cy);
                if (py >= 0 && py < dstH)
                {
                    for (int cx = 0; cx < srcW; cx++)
                    {
                        int px = xFlip ? (x + (srcW - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < dstW)
                        {
                            DrawPoint_Unchecked(GetPixelAddress(dst, dstW, px, py), src(srcW - cx - 1, srcH - cy - 1));
                        }
                    }
                }
            }
        }
        public static void DrawBitmapRotated270CW(uint* dst, int dstW, int dstH, int x, int y, PixelSupplier src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
        {
            for (int cy = 0; cy < srcW; cy++)
            {
                int py = yFlip ? (y + (srcH - 1 - cy)) : (y + cy);
                if (py >= 0 && py < dstH)
                {
                    for (int cx = 0; cx < srcH; cx++)
                    {
                        int px = xFlip ? (x + (srcW - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < dstW)
                        {
                            DrawPoint_Unchecked(GetPixelAddress(dst, dstW, px, py), src(srcW - cy - 1, cx));
                        }
                    }
                }
            }
        }

        #endregion

        #region Arbitrary number of degrees

        private static void TransformSurface90(PixelSupplier src, int srcW, int srcH, uint* dst, int dstW, int dstH,
            int angle90, bool xFlip, bool yFlip)
        {
            switch (angle90)
            {
                case 0: OverwriteBitmap(dst, dstW, dstH, 0, 0, src, srcW, srcH, xFlip: xFlip, yFlip: yFlip); break; // 0
                case 1: OverwriteBitmapRotated90CW(dst, dstW, dstH, 0, 0, src, srcW, srcH, xFlip: xFlip, yFlip: yFlip); break; // 90
                case 2: OverwriteBitmapRotated180CW(dst, dstW, dstH, 0, 0, src, srcW, srcH, xFlip: xFlip, yFlip: yFlip); break; // 190
                default: OverwriteBitmapRotated270CW(dst, dstW, dstH, 0, 0, src, srcW, srcH, xFlip: xFlip, yFlip: yFlip); break; // 270
            }
        }

        private static void TransformSurface(PixelSupplier src, int srcW, int srcH, uint* dst, int dstW, int dstH,
            int centerX, int centerY, int isin, int icos, bool xFlip, bool yFlip, bool smooth)
        {
            int xd = (srcW - dstW) << 15; // << 15 is *32768
            int yd = (srcH - dstH) << 15;
            int ax = (centerX << 16) - (icos * centerX); // << 16 is *65536
            int ay = (centerY << 16) - (isin * centerX);
            int sw = srcW - 1;
            int sh = srcH - 1;

            if (smooth)
            {
                for (int y = 0; y < dstH; y++)
                {
                    int dy = centerY - y;
                    int sdx = ax + (isin * dy) + xd;
                    int sdy = ay - (icos * dy) + yd;
                    for (int x = 0; x < dstW; x++)
                    {
                        int dx = sdx >> 16;
                        dy = sdy >> 16;
                        if (xFlip)
                        {
                            dx = sw - dx;
                        }
                        if (yFlip)
                        {
                            dy = sh - dy;
                        }
                        if ((dx > -1) && (dy > -1) && (dx < (srcW - 1)) && (dy < (srcH - 1)))
                        {
                            // Get 4 pixels
                            uint c00 = src(dx, dy);
                            uint c01 = src(dx + 1, dy);
                            uint c10 = src(dx, dy + 1);
                            uint c11 = src(dx + 1, dy + 1);
                            if (xFlip)
                            {
                                uint cswap = c00; c00 = c01; c01 = cswap;
                                cswap = c10; c10 = c11; c11 = cswap;
                            }
                            if (yFlip)
                            {
                                uint cswap = c00; c00 = c10; c10 = cswap;
                                cswap = c01; c01 = c11; c11 = cswap;
                            }
                            // Interpolate colors from the 4 pixels
                            int ex = sdx & 0xffff;
                            int ey = sdy & 0xffff;
                            uint color = 0;
                            byte b0 = (byte)GetR(c00);
                            byte b1 = (byte)GetR(c01);
                            int t1 = ((((b1 - b0) * ex) >> 16) + b0) & 0xff;
                            b0 = (byte)GetR(c10);
                            b1 = (byte)GetR(c11);
                            int t2 = ((((b1 - b0) * ex) >> 16) + b0) & 0xff;
                            color = SetR(color, (uint)((((t2 - t1) * ey) >> 16) + t1));
                            b0 = (byte)GetG(c00);
                            b1 = (byte)GetG(c01);
                            t1 = ((((b1 - b0) * ex) >> 16) + b0) & 0xff;
                            b0 = (byte)GetG(c10);
                            b1 = (byte)GetG(c11);
                            t2 = ((((b1 - b0) * ex) >> 16) + b0) & 0xff;
                            color = SetG(color, (uint)((((t2 - t1) * ey) >> 16) + t1));
                            b0 = (byte)GetB(c00);
                            b1 = (byte)GetB(c01);
                            t1 = ((((b1 - b0) * ex) >> 16) + b0) & 0xff;
                            b0 = (byte)GetB(c10);
                            b1 = (byte)GetB(c11);
                            t2 = ((((b1 - b0) * ex) >> 16) + b0) & 0xff;
                            color = SetB(color, (uint)((((t2 - t1) * ey) >> 16) + t1));
                            b0 = (byte)GetA(c00);
                            b1 = (byte)GetA(c01);
                            t1 = ((((b1 - b0) * ex) >> 16) + b0) & 0xff;
                            b0 = (byte)GetA(c10);
                            b1 = (byte)GetA(c11);
                            t2 = ((((b1 - b0) * ex) >> 16) + b0) & 0xff;
                            color = SetA(color, (uint)((((t2 - t1) * ey) >> 16) + t1));
                            *dst = color;
                        }
                        sdx += icos;
                        sdy += isin;
                        dst++;
                    }
                }
            }
            else
            {
                for (int y = 0; y < dstH; y++)
                {
                    int dy = centerY - y;
                    int sdx = ax + (isin * dy) + xd;
                    int sdy = ay - (icos * dy) + yd;
                    for (int x = 0; x < dstW; x++)
                    {
                        int dx = sdx >> 16; // /65536
                        dy = sdy >> 16;
                        if ((uint)dx < (uint)srcW && (uint)dy < (uint)srcH)
                        {
                            if (xFlip)
                            {
                                dx = sw - dx;
                            }
                            if (yFlip)
                            {
                                dy = sh - dy;
                            }
                            *dst = src(dx, dy);
                        }
                        sdx += icos;
                        sdy += isin;
                        dst++;
                    }
                }
            }
        }

        private static void CalcSurfaceSize(int width, int height, double angle,
            out int dstW, out int dstH, out double cAngle, out double sAngle)
        {
            double a = angle / 90;
            int angle90 = (int)a;
            if (angle90 == a)
            {
                angle90 %= 4;
                if (angle90 < 0)
                {
                    angle90 += 4; // 0:0 deg, 1:90 deg, 2:180 deg, 3:270 deg
                }
                if ((angle90 & 1) != 0)
                {
                    dstW = height;
                    dstH = width;
                    cAngle = 0;
                    sAngle = angle90 == 1 ? -1 : 1; // Reversed because our rotations are clockwise
                }
                else
                {
                    dstW = width;
                    dstH = height;
                    cAngle = angle90 == 0 ? 1 : -1;
                    sAngle = 0;
                }
            }
            else
            {
                // Determine destination width and height by rotating a centered source box
                double radians = angle * (Math.PI / -180.0); // Reverse the angle because our rotations are clockwise
                sAngle = Math.Sin(radians);
                cAngle = Math.Cos(radians);
                double x = width / 2;
                double y = height / 2;
                double cx = cAngle * x;
                double cy = cAngle * y;
                double sx = sAngle * x;
                double sy = sAngle * y;

                int dstwidthhalf = Math.Max((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(Math.Abs(cx + sy), Math.Abs(cx - sy)), Math.Abs(-cx + sy)), Math.Abs(-cx - sy))), 1);
                int dstheighthalf = Math.Max((int)Math.Ceiling(Math.Max(Math.Max(Math.Max(Math.Abs(sx + cy), Math.Abs(sx - cy)), Math.Abs(-sx + cy)), Math.Abs(-sx - cy))), 1);
                dstW = 2 * dstwidthhalf;
                dstH = 2 * dstheighthalf;
            }
        }

        private static uint[] RotateSurface(PixelSupplier src, int srcW, int srcH, double angle, int centerX, int centerY, bool smooth, bool xFlip, bool yFlip,
            int dstW, int dstH, double cAngle, double sAngle)
        {
            uint[] dstArray = new uint[dstW * (dstH + 2)]; // 2 is extra tolerance space for some rotations
            fixed (uint* dst = dstArray)
            {
                double a = angle / 90;
                int angle90 = (int)a;
                if (angle90 == a)
                {
                    angle90 %= 4;
                    if (angle90 < 0) // Negative angles
                    {
                        angle90 += 4;
                    }
                    TransformSurface90(src, srcW, srcH, dst, dstW, dstH, angle90, xFlip, yFlip);
                }
                else
                {
                    double sAngleInv = sAngle * 0x10000;
                    double cAngleInv = cAngle * 0x10000;
                    TransformSurface(src, srcW, srcH, dst, dstW, dstH, centerX, centerY,
                        (int)sAngleInv, (int)cAngleInv, xFlip, yFlip, smooth);
                }
            }
            return dstArray;
        }

        public static uint[] CreateRotatedBitmap(PixelSupplier src, int srcW, int srcH, double angle,
            out int rotWidth, out int rotHeight, out double cAngle, out double sAngle,
            bool xFlip = false, bool yFlip = false, bool smooth = false)
        {
            CalcSurfaceSize(srcW, srcH, angle, out rotWidth, out rotHeight, out cAngle, out sAngle);
            return RotateSurface(src, srcW, srcH, angle, rotWidth / 2, rotHeight / 2, smooth, xFlip, yFlip, rotWidth, rotHeight, cAngle, sAngle);
        }

        public static void DrawRotatedBitmap(uint* dst, int dstW, int dstH, int x, int y, PixelSupplier src, int srcW, int srcH, double angle, bool xFlip = false, bool yFlip = false, bool smooth = false)
        {
            fixed (uint* rotated = CreateRotatedBitmap(src, srcW, srcH, angle, out int rotWidth, out int rotHeight, out _, out _, xFlip: xFlip, yFlip: yFlip, smooth: smooth))
            {
                PixelSupplier pixSupplyRotated = MakeBitmapSupplier(rotated, rotWidth);
                DrawBitmap(dst, dstW, dstH, x, y, pixSupplyRotated, rotWidth, rotHeight);
            }
        }
        public static void DrawRotatedBitmap_Centered(uint* dst, int dstW, int dstH, int x, int y, PixelSupplier src, int srcW, int srcH, double angle, bool xFlip = false, bool yFlip = false, bool smooth = false)
        {
            fixed (uint* rotated = CreateRotatedBitmap(src, srcW, srcH, angle, out int rotWidth, out int rotHeight, out double cAngle, out double sAngle, xFlip: xFlip, yFlip: yFlip, smooth: smooth))
            {
                PixelSupplier pixSupplyRotated = MakeBitmapSupplier(rotated, rotWidth);

                double centerX = srcW / 2;
                double centerY = srcH / 2;

                // Find out where the new origin is by rotating the four final_rect points around the center and then taking the extremes
                int abscenterx = x + (int)centerX;
                int abscentery = y + (int)centerY;
                sAngle = -sAngle;

                // Top Left
                double px = x - abscenterx;
                double py = y - abscentery;
                double p1x = px * cAngle - py * sAngle + abscenterx;
                double p1y = px * sAngle + py * cAngle + abscentery;

                // Top Right
                px = x + srcW - abscenterx;
                py = y - abscentery;
                double p2x = px * cAngle - py * sAngle + abscenterx;
                double p2y = px * sAngle + py * cAngle + abscentery;

                // Bottom Left
                px = x - abscenterx;
                py = y + srcH - abscentery;
                double p3x = px * cAngle - py * sAngle + abscenterx;
                double p3y = px * sAngle + py * cAngle + abscentery;

                // Bottom Right
                px = x + srcW - abscenterx;
                py = y + srcH - abscentery;
                double p4x = px * cAngle - py * sAngle + abscenterx;
                double p4y = px * sAngle + py * cAngle + abscentery;

                x = (int)Math.Min(Math.Min(p1x, p2x), Math.Min(p3x, p4x));
                y = (int)Math.Min(Math.Min(p1y, p2y), Math.Min(p3y, p4y));

                DrawBitmap(dst, dstW, dstH, x, y, pixSupplyRotated, rotWidth, rotHeight);
            }
        }

        #endregion
    }
}
