using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Kermalis.MapEditor.Util
{
    internal sealed class RenderUtils
    {
        public static WriteableBitmap ToWriteableBitmap(Bitmap bmp)
        {
            var wb =  new WriteableBitmap(bmp.PixelSize, bmp.Dpi, PixelFormat.Bgra8888);
            using (IRenderTarget rtb = Utils.RenderInterface.CreateRenderTarget(new[] { new WriteableBitmapSurface(wb) }))
            using (IDrawingContextImpl ctx = rtb.CreateDrawingContext(null))
            {
                var rect = new Rect(bmp.Size);
                ctx.DrawImage(bmp.PlatformImpl, 1, rect, rect);
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

        public static unsafe void DrawCrossUnchecked(uint* bmpAddress, int bmpWidth, int x, int y, int width, int height, uint color)
        {
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    if (px == py)
                    {
                        DrawUnchecked(bmpAddress + x + px + ((y + py) * bmpWidth), color);
                        DrawUnchecked(bmpAddress + x + (width - 1 - px) + ((y + py) * bmpWidth), color);
                    }
                }
            }
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
        public static unsafe void DrawUnchecked(uint* pixelAddress, uint color)
        {
            if (color == 0x00000000)
            {
                return;
            }
            uint aA = color >> 24;
            if (aA == 0xFF)
            {
                *pixelAddress = color;
            }
            else
            {
                uint rA = (color >> 16) & 0xFF;
                uint gA = (color >> 8) & 0xFF;
                uint bA = color & 0xFF;
                uint current = *pixelAddress;
                uint aB = current >> 24;
                uint rB = (current >> 16) & 0xFF;
                uint gB = (current >> 8) & 0xFF;
                uint bB = current & 0xFF;
                uint a = aA + (aB * (0xFF - aA) / 0xFF);
                uint r = (rA * aA / 0xFF) + (rB * aB * (0xFF - aA) / (0xFF * 0xFF));
                uint g = (gA * aA / 0xFF) + (gB * aB * (0xFF - aA) / (0xFF * 0xFF));
                uint b = (bA * aA / 0xFF) + (bB * aB * (0xFF - aA) / (0xFF * 0xFF));
                *pixelAddress = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }
    }
}
