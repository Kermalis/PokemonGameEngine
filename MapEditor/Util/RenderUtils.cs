using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace Kermalis.MapEditor.Util
{
    internal sealed class RenderUtils
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

        public static unsafe uint[][] LoadBitmapSheet(string fileName, int spriteWidth, int spriteHeight, out int sheetWidth, out int sheetHeight)
        {
            using (WriteableBitmap wb = ToWriteableBitmap(new Bitmap(fileName)))
            using (ILockedFramebuffer l = wb.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
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
                        sprites[sprite++] = GetBitmapUnchecked(bmpAddress, sheetWidth, sx * spriteWidth, sy * spriteHeight, spriteWidth, spriteHeight);
                    }
                }
                return sprites;
            }
        }
        public static unsafe uint[] GetBitmapUnchecked(uint* bmpAddress, int bmpWidth, int x, int y, int width, int height)
        {
            uint[] arr = new uint[width * height];
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    arr[px + (py * width)] = *(bmpAddress + (x + px) + ((y + py) * bmpWidth));
                }
            }
            return arr;
        }

        public static unsafe void TransparencyGrid(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int blockW, int blockH, int numX, int numY)
        {
            for (int by = 0; by < numY; by++)
            {
                for (int bx = 0; bx < numX; bx++)
                {
                    FillColor(bmpAddress, bmpWidth, bmpHeight, (bx * blockW) + x, (by * blockH) + y, blockW, blockH, (bx + by) % 2 == 0 ? 0xFFBFBFBF : 0xFFFFFFFF);
                }
            }
        }
        public static unsafe void TransparencyGrid(uint* bmpAddress, int bmpWidth, int bmpHeight, int blockW, int blockH)
        {
            TransparencyGrid(bmpAddress, bmpWidth, bmpHeight, 0, 0, blockW, blockH, (bmpWidth / blockW) + (bmpWidth % blockW == 0 ? 0 : 1), (bmpHeight / blockH) + (bmpHeight % blockH == 0 ? 0 : 1));
        }

        public static unsafe void FillColor(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, int height, uint color)
        {
            for (int py = y; py < y + height; py++)
            {
                if (py >= 0 && py < bmpHeight)
                {
                    for (int px = x; px < x + width; px++)
                    {
                        if (px >= 0 && px < bmpWidth)
                        {
                            DrawUnchecked(bmpAddress + px + (py * bmpWidth), color);
                        }
                    }
                }
            }
        }

        public static unsafe void DrawBitmap(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, uint[] otherBmp, int otherBmpWidth, int otherBmpHeight, bool xFlip = false, bool yFlip = false)
        {
            fixed (uint* otherBmpAddress = otherBmp)
            {
                DrawBitmap(bmpAddress, bmpWidth, bmpHeight, x, y, otherBmpAddress, otherBmpWidth, otherBmpHeight, xFlip: xFlip, yFlip: yFlip);
            }
        }
        public static unsafe void DrawBitmap(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, bool xFlip = false, bool yFlip = false)
        {
            for (int cy = 0; cy < otherBmpHeight; cy++)
            {
                int py = yFlip ? (y + (otherBmpHeight - 1 - cy)) : (y + cy);
                if (py >= 0 && py < bmpHeight)
                {
                    for (int cx = 0; cx < otherBmpWidth; cx++)
                    {
                        int px = xFlip ? (x + (otherBmpWidth - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < bmpWidth)
                        {
                            DrawUnchecked(bmpAddress + px + (py * bmpWidth), *(otherBmpAddress + cx + (cy * otherBmpWidth)));
                        }
                    }
                }
            }
        }

        public static unsafe void DrawHorizontalLine(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, uint color)
        {
            if (y < 0 || y >= bmpHeight)
            {
                return;
            }
            int target = x + width;
            for (int px = x; px < target; px++)
            {
                if (px >= 0 && px < bmpWidth)
                {
                    DrawUnchecked(bmpAddress + px + (y * bmpWidth), color);
                }
            }
        }
        public static unsafe void DrawVerticalLine(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int height, uint color)
        {
            if (x < 0 || x >= bmpWidth)
            {
                return;
            }
            int target = y + height;
            for (int py = y; py < target; py++)
            {
                if (py >= 0 && py < bmpHeight)
                {
                    DrawUnchecked(bmpAddress + x + (py * bmpWidth), color);
                }
            }
        }
        // Bresenham's line algorithm
        public static unsafe void DrawLineLow(uint* bmpAddress, int bmpWidth, int bmpHeight, int x1, int y1, int x2, int y2, uint color)
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
                if (px >= 0 && px < bmpWidth && py >= 0 && py < bmpHeight)
                {
                    DrawUnchecked(bmpAddress + px + (py * bmpWidth), color);
                }
                if (d > 0)
                {
                    py += yi;
                    d -= 2 * dx;
                }
                d += 2 * dy;
            }
        }
        public static unsafe void DrawLineHigh(uint* bmpAddress, int bmpWidth, int bmpHeight, int x1, int y1, int x2, int y2, uint color)
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
                if (px >= 0 && px < bmpWidth && py >= 0 && py < bmpHeight)
                {
                    DrawUnchecked(bmpAddress + px + (py * bmpWidth), color);
                }
                if (d > 0)
                {
                    px += xi;
                    d -= 2 * dy;
                }
                d += 2 * dx;
            }
        }
        public static unsafe void DrawLine(uint* bmpAddress, int bmpWidth, int bmpHeight, int x1, int y1, int x2, int y2, uint color)
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
                DrawVerticalLine(bmpAddress, bmpWidth, bmpHeight, x1, y, height, color);
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
                DrawVerticalLine(bmpAddress, bmpWidth, bmpHeight, x, y1, width, color);
            }
            else if (Math.Abs(y2 - y1) < Math.Abs(x2 - x1))
            {
                if (x1 > x2)
                {
                    DrawLineLow(bmpAddress, bmpWidth, bmpHeight, x2, y2, x1, y1, color);
                }
                else
                {
                    DrawLineLow(bmpAddress, bmpWidth, bmpHeight, x1, y1, x2, y2, color);
                }
            }
            else
            {
                if (y1 > y2)
                {
                    DrawLineHigh(bmpAddress, bmpWidth, bmpHeight, x2, y2, x1, y1, color);
                }
                else
                {
                    DrawLineHigh(bmpAddress, bmpWidth, bmpHeight, x1, y1, x2, y2, color);
                }
            }
        }
        public static unsafe void DrawCross(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, int height, uint color)
        {
            int x2 = x + width - 1;
            int y2 = y + height - 1;
            DrawLine(bmpAddress, bmpWidth, bmpHeight, x, y, x2, y2, color);
            DrawLine(bmpAddress, bmpWidth, bmpHeight, x, y2, x2, y, color);
        }

        public static unsafe void ClearUnchecked(uint* bmpAddress, int bmpWidth, int x, int y, int width, int height)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    uint* pixelAddress = bmpAddress + px + (py * bmpWidth);
                    *pixelAddress = 0x00000000;
                }
            }
        }
        // Colors must be RGBA8888 (0xAABBCCDD - AA is A, BB is B, CC is G, DD is R)
        public static unsafe void DrawUnchecked(uint* pixelAddress, uint color)
        {
            uint aA = color >> 24;
            if (aA == 0)
            {
                return; // Fully transparent
            }
            else if (aA == 0xFF)
            {
                *pixelAddress = color; // Fully opaque
            }
            else
            {
                uint bA = (color >> 16) & 0xFF;
                uint gA = (color >> 8) & 0xFF;
                uint rA = color & 0xFF;
                uint current = *pixelAddress;
                uint aB = current >> 24;
                uint bB = (current >> 16) & 0xFF;
                uint gB = (current >> 8) & 0xFF;
                uint rB = current & 0xFF;
                uint a = aA + (aB * (0xFF - aA) / 0xFF);
                uint r = (rA * aA / 0xFF) + (rB * aB * (0xFF - aA) / (0xFF * 0xFF));
                uint g = (gA * aA / 0xFF) + (gB * aB * (0xFF - aA) / (0xFF * 0xFF));
                uint b = (bA * aA / 0xFF) + (bB * aB * (0xFF - aA) / (0xFF * 0xFF));
                *pixelAddress = (a << 24) | (b << 16) | (g << 8) | r;
            }
        }
    }
}
