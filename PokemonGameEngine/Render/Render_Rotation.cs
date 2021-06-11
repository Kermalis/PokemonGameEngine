using System;

namespace Kermalis.PokemonGameEngine.Render
{
    // https://github.com/spurious/SDL-mirror/blob/master/src/render/software/SDL_rotate.c
    // https://github.com/spurious/SDL-mirror/blob/master/src/render/software/SDL_render_sw.c
    internal static partial class RenderUtils
    {
        #region 90 degree multiples

        public static unsafe void DrawBitmapRotated90CW(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight)
        {
            for (int cy = 0; cy < otherBmpWidth; cy++)
            {
                int py = y + cy;
                if (py >= 0 && py < bmpHeight)
                {
                    for (int cx = 0; cx < otherBmpHeight; cx++)
                    {
                        int px = x + cx;
                        if (px >= 0 && px < bmpWidth)
                        {
                            DrawUnchecked(bmpAddress, bmpWidth, px, py, *GetPixelAddress(otherBmpAddress, otherBmpWidth, cy, otherBmpHeight - cx - 1));
                        }
                    }
                }
            }
        }
        public static unsafe void DrawBitmapRotated180CW(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight)
        {
            for (int cy = 0; cy < otherBmpHeight; cy++)
            {
                int py = y + cy;
                if (py >= 0 && py < bmpHeight)
                {
                    for (int cx = 0; cx < otherBmpWidth; cx++)
                    {
                        int px = x + cx;
                        if (px >= 0 && px < bmpWidth)
                        {
                            DrawUnchecked(bmpAddress, bmpWidth, px, py, *GetPixelAddress(otherBmpAddress, otherBmpWidth, otherBmpWidth - cx - 1, otherBmpHeight - cy - 1));
                        }
                    }
                }
            }
        }
        public static unsafe void DrawBitmapRotated270CW(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight)
        {
            for (int cy = 0; cy < otherBmpWidth; cy++)
            {
                int py = y + cy;
                if (py >= 0 && py < bmpHeight)
                {
                    for (int cx = 0; cx < otherBmpHeight; cx++)
                    {
                        int px = x + cx;
                        if (px >= 0 && px < bmpWidth)
                        {
                            DrawUnchecked(bmpAddress, bmpWidth, px, py, *GetPixelAddress(otherBmpAddress, otherBmpWidth, otherBmpWidth - cy - 1, cx));
                        }
                    }
                }
            }
        }

        #endregion

        #region Arbitrary degrees

        // Computes source pointer X/Y increments for a rotation that's a multiple of 90 degrees.
        private static unsafe void ComputeSourceIncrements90(int srcW, int srcH, int angle90, bool xFlip, bool yFlip,
            out int incX, out int incY, out int signX, out int signY)
        {
            int p = yFlip ? -srcW : srcW;
            int b = 1;
            if (xFlip)
            {
                b = -b;
            }
            switch (angle90)
            {
                case 0: incX = b; incY = p - (srcW * b); signX = 1; signY = 1; break; // 0
                case 1: incX = -p; incY = b - (-p * srcH); signX = 1; signY = -1; break; // 90
                case 2: incX = -b; incY = (-srcW * -b) - p; signX = -1; signY = -1; break; // 180
                default: incX = p; incY = (-p * srcH) - b; signX = -1; signY = 1; break; // 270
            }
            if (xFlip)
            {
                signX = -signX;
            }
            if (yFlip)
            {
                signY = -signY;
            }
        }

        // Performs a relatively fast rotation/flip when the angle is a multiple of 90 degrees.
        private static unsafe void TransformSurface90(uint* src, int srcW, int srcH, uint* dst, int dstW, int dstH,
            int angle90, bool xFlip, bool yFlip)
        {
            ComputeSourceIncrements90(srcW, srcH, angle90, xFlip, yFlip, out int incX, out int incY, out int signX, out int signY);
            if (signX < 0)
            {
                src += srcW - 1;
            }
            if (signY < 0)
            {
                src += (srcH - 1) * srcW;
            }

            for (int dy = 0; dy < dstH; dy++)
            {
                if (incX == 1)
                {
                    Buffer.MemoryCopy(src, dst, dstW * sizeof(uint), dstW * sizeof(uint));
                    src += dstW;
                    dst += dstW;
                }
                else
                {
                    for (uint* de = dst + dstW; dst != de; src += incX, dst++)
                    {
                        *dst = *src;
                    }
                }
                src += incY;
            }
        }

        private static unsafe void TransformSurface(uint* src, int srcW, int srcH, uint* dst, int dstW, int dstH,
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
                            uint* sp = GetPixelAddress(src, srcW, dx, dy);
                            uint c00 = *sp;
                            sp++;
                            uint c01 = *sp;
                            sp += srcW;
                            uint c11 = *sp;
                            sp--;
                            uint c10 = *sp;
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
                            *dst = *GetPixelAddress(src, srcW, dx, dy);
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

        private static unsafe uint[] RotateSurface(uint* src, int srcW, int srcH, double angle, int centerX, int centerY, bool smooth, bool xFlip, bool yFlip,
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

        public static unsafe void DrawRotatedBitmap(uint* dst, int dstW, int dstH, int x, int y, uint* src, int srcW, int srcH, double angle, bool xFlip = false, bool yFlip = false, bool smooth = false)
        {
            CalcSurfaceSize(srcW, srcH, angle, out int dstWidth, out int dstHeight, out double cAngle, out double sAngle);
            uint[] rotated = RotateSurface(src, srcW, srcH, angle, dstWidth / 2, dstHeight / 2, smooth, xFlip, yFlip, dstWidth, dstHeight, cAngle, sAngle);

            DrawBitmap(dst, dstW, dstH, x, y, rotated, dstWidth, dstHeight);
        }

        public static unsafe void DrawRotatedBitmap_Centered(uint* dst, int dstW, int dstH, int x, int y, uint* src, int srcW, int srcH, double angle, bool xFlip = false, bool yFlip = false, bool smooth = false)
        {
            CalcSurfaceSize(srcW, srcH, angle, out int dstWidth, out int dstHeight, out double cAngle, out double sAngle);
            uint[] rotated = RotateSurface(src, srcW, srcH, angle, dstWidth / 2, dstHeight / 2, smooth, xFlip, yFlip, dstWidth, dstHeight, cAngle, sAngle);

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

            DrawBitmap(dst, dstW, dstH, x, y, rotated, dstWidth, dstHeight);
        }

        #endregion
    }
}
