using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Runtime.CompilerServices;

namespace Kermalis.MapEditor.Util
{
    internal static unsafe class Renderer
    {
        public static WriteableBitmap ToWriteableBitmap(Bitmap bmp)
        {
            var wb =  new WriteableBitmap(bmp.PixelSize, bmp.Dpi, PixelFormat.Rgba8888, AlphaFormat.Premul);
            using (IRenderTarget rtb = Utils.RenderInterface.CreateRenderTarget(new[] { new WriteableBitmapSurface(wb) }))
            using (IDrawingContextImpl ctx = rtb.CreateDrawingContext(null))
            {
                var rect = new Rect(bmp.Size);
                ctx.DrawBitmap(bmp.PlatformImpl, 1, rect, rect);
            }
            bmp.Dispose();
            return wb;
        }

        public static uint[][] LoadBitmapSheet(string fileName, int spriteWidth, int spriteHeight, out int sheetWidth, out int sheetHeight)
        {
            using (WriteableBitmap wb = ToWriteableBitmap(new Bitmap(fileName)))
            using (ILockedFramebuffer l = wb.Lock())
            {
                uint* src = (uint*)l.Address.ToPointer();
                PixelSize ps = wb.PixelSize;
                sheetWidth = ps.Width;
                sheetHeight = ps.Height;
                int numSpritesX = sheetWidth / spriteWidth;
                int numSpritesY = sheetHeight / spriteHeight;
                uint[][] sprites = new uint[numSpritesX * numSpritesY][];
                int sprite = 0;
                for (int sy = 0; sy < numSpritesY; sy++)
                {
                    for (int sx = 0; sx < numSpritesX; sx++)
                    {
                        sprites[sprite++] = GetBitmap_Unchecked(src, sheetWidth, sx * spriteWidth, sy * spriteHeight, spriteWidth, spriteHeight);
                    }
                }
                return sprites;
            }
        }
        public static uint[] GetBitmap_Unchecked(uint* src, int srcW, int x, int y, int width, int height)
        {
            uint[] arr = new uint[width * height];
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    arr[px + (py * width)] = *(src + (x + px) + ((y + py) * srcW));
                }
            }
            return arr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TransparencyGrid(uint* dst, int dstW, int dstH, int blockW, int blockH)
        {
            TransparencyGrid(dst, dstW, dstH, 0, 0, blockW, blockH, (dstW / blockW) + (dstW % blockW == 0 ? 0 : 1), (dstH / blockH) + (dstH % blockH == 0 ? 0 : 1));
        }
        public static void TransparencyGrid(uint* dst, int dstW, int dstH, int x, int y, int blockW, int blockH, int numX, int numY)
        {
            for (int by = 0; by < numY; by++)
            {
                for (int bx = 0; bx < numX; bx++)
                {
                    FillRectangle(dst, dstW, dstH, (bx * blockW) + x, (by * blockH) + y, blockW, blockH, (bx + by) % 2 == 0 ? Color(192, 192, 192, 255) : Color(255, 255, 255, 255));
                }
            }
        }

        public static void ClearRectangle_Unchecked(uint* dst, int dstW, int x, int y, int width, int height)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    *GetPixelAddress(dst, dstW, px, py) = 0;
                }
            }
        }
        public static void FillRectangle(uint* dst, int dstW, int dstH, int x, int y, int width, int height, uint color)
        {
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
        public static void DrawBitmap(uint* dst, int dstW, int dstH, int x, int y, uint[] srcBmp, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
        {
            fixed (uint* src = srcBmp)
            {
                DrawBitmap(dst, dstW, dstH, x, y, src, srcW, srcH, xFlip: xFlip, yFlip: yFlip);
            }
        }
        public static void DrawBitmap(uint* dst, int dstW, int dstH, int x, int y, uint* src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
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
                            DrawPoint_Unchecked(dst + px + (py * dstW), *(src + cx + (cy * srcW)));
                        }
                    }
                }
            }
        }

        public static void DrawHorizontalLine(uint* dst, int dstW, int dstH, int x, int y, int width, uint color)
        {
            if (y < 0 || y >= dstH)
            {
                return;
            }
            int target = x + width;
            for (int px = x; px < target; px++)
            {
                if (px >= 0 && px < dstW)
                {
                    DrawPoint_Unchecked(dst + px + (y * dstW), color);
                }
            }
        }
        public static void DrawVerticalLine(uint* dst, int dstW, int dstH, int x, int y, int height, uint color)
        {
            if (x < 0 || x >= dstW)
            {
                return;
            }
            int target = y + height;
            for (int py = y; py < target; py++)
            {
                if (py >= 0 && py < dstH)
                {
                    DrawPoint_Unchecked(dst + x + (py * dstW), color);
                }
            }
        }
        // Bresenham's line algorithm
        public static void DrawLineLow(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, uint color)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            int yi = 1;
            if (dy < 0)
            {
                yi = -1;
                dy = -dy;
            }
            int d = 2 * dy - dx;
            int py = y1;
            for (int px = x1; px <= x2; px++)
            {
                if (px >= 0 && px < dstW && py >= 0 && py < dstH)
                {
                    DrawPoint_Unchecked(dst + px + (py * dstW), color);
                }
                if (d > 0)
                {
                    py += yi;
                    d -= 2 * dx;
                }
                d += 2 * dy;
            }
        }
        public static void DrawLineHigh(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, uint color)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            int xi = 1;
            if (dx < 0)
            {
                xi = -1;
                dx = -dx;
            }
            int d = 2 * dx - dy;
            int px = x1;
            for (int py = y1; py <= y2; py++)
            {
                if (px >= 0 && px < dstW && py >= 0 && py < dstH)
                {
                    DrawPoint_Unchecked(dst + px + (py * dstW), color);
                }
                if (d > 0)
                {
                    px += xi;
                    d -= 2 * dy;
                }
                d += 2 * dx;
            }
        }
        public static void DrawLine(uint* dst, int dstW, int dstH, int x1, int y1, int x2, int y2, uint color)
        {
            if (x1 == x2)
            {
                int y;
                int height;
                if (y1 < y2)
                {
                    y = y1;
                    height = y2 - y1;
                }
                else
                {
                    y = y2;
                    height = y1 - y2;
                }
                DrawVerticalLine(dst, dstW, dstH, x1, y, height, color);
            }
            else if (y1 == y2)
            {
                int x;
                int width;
                if (x1 < x2)
                {
                    x = x1;
                    width = x2 - x1;
                }
                else
                {
                    x = x2;
                    width = x1 - x2;
                }
                DrawVerticalLine(dst, dstW, dstH, x, y1, width, color);
            }
            else if (Math.Abs(y2 - y1) < Math.Abs(x2 - x1))
            {
                if (x1 > x2)
                {
                    DrawLineLow(dst, dstW, dstH, x2, y2, x1, y1, color);
                }
                else
                {
                    DrawLineLow(dst, dstW, dstH, x1, y1, x2, y2, color);
                }
            }
            else
            {
                if (y1 > y2)
                {
                    DrawLineHigh(dst, dstW, dstH, x2, y2, x1, y1, color);
                }
                else
                {
                    DrawLineHigh(dst, dstW, dstH, x1, y1, x2, y2, color);
                }
            }
        }
        public static void DrawCross(uint* dst, int dstW, int dstH, int x, int y, int width, int height, uint color)
        {
            int x2 = x + width - 1;
            int y2 = y + height - 1;
            DrawLine(dst, dstW, dstH, x, y, x2, y2, color);
            DrawLine(dst, dstW, dstH, x, y2, x2, y, color);
        }

        // Colors must be RGBA8888 (0xAABBCCDD - AA is A, BB is B, CC is G, DD is R)
        public static void DrawPoint_Unchecked(uint* dst, uint color)
        {
            uint aA = color >> 24;
            if (aA == 0)
            {
                return; // Fully transparent
            }
            else if (aA == 0xFF)
            {
                *dst = color; // Fully opaque
            }
            else
            {
                uint bA = (color >> 16) & 0xFF;
                uint gA = (color >> 8) & 0xFF;
                uint rA = color & 0xFF;
                uint current = *dst;
                uint aB = current >> 24;
                uint bB = (current >> 16) & 0xFF;
                uint gB = (current >> 8) & 0xFF;
                uint rB = current & 0xFF;
                uint a = aA + (aB * (0xFF - aA) / 0xFF);
                uint r = (rA * aA / 0xFF) + (rB * aB * (0xFF - aA) / (0xFF * 0xFF));
                uint g = (gA * aA / 0xFF) + (gB * aB * (0xFF - aA) / (0xFF * 0xFF));
                uint b = (bA * aA / 0xFF) + (bB * aB * (0xFF - aA) / (0xFF * 0xFF));
                *dst = (a << 24) | (b << 16) | (g << 8) | r;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint* GetPixelAddress(uint* src, int srcW, int x, int y)
        {
            return src + x + (y * srcW);
        }

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
