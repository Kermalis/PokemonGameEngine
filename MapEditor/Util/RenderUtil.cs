using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Kermalis.MapEditor.Util
{
    internal sealed class RenderUtil
    {
        public static unsafe uint[][][] LoadSpriteSheet(string fileName, int spriteWidth, int spriteHeight)
        {
            var bmp = new Bitmap(fileName);
            using (var wb = new WriteableBitmap(bmp.PixelSize, bmp.Dpi, PixelFormat.Bgra8888))
            {
                using (IRenderTarget rtb = Utils.RenderInterface.CreateRenderTarget(new[] { new WriteableBitmapSurface(wb) }))
                using (IDrawingContextImpl ctx = rtb.CreateDrawingContext(null))
                {
                    var rect = new Rect(bmp.Size);
                    ctx.DrawImage(bmp.PlatformImpl, 1, rect, rect);
                }
                bmp.Dispose();
                uint[][][] sprites;
                using (ILockedFramebuffer l = wb.Lock())
                {
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    PixelSize ps = wb.PixelSize;
                    int numSpritesX = ps.Width / spriteWidth;
                    int numSpritesY = ps.Height / spriteHeight;
                    sprites = new uint[numSpritesX * numSpritesY][][];
                    int sprite = 0;
                    for (int sy = 0; sy < numSpritesY; sy++)
                    {
                        for (int sx = 0; sx < numSpritesX; sx++)
                        {
                            uint[][] arrY = new uint[spriteHeight][];
                            for (int py = 0; py < spriteHeight; py++)
                            {
                                uint[] arrX = new uint[spriteWidth];
                                for (int px = 0; px < spriteWidth; px++)
                                {
                                    arrX[px] = *(bmpAddress + ((sx * spriteWidth) + px) + (((sy * spriteHeight) + py) * ps.Width));
                                }
                                arrY[py] = arrX;
                            }
                            sprites[sprite++] = arrY;
                        }
                    }
                }
                return sprites;
            }
        }

        public static unsafe void TransparencyGrid(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int blockW, int blockH, int numX, int numY)
        {
            for (int by = 0; by < numY; by++)
            {
                for (int bx = 0; bx < numX; bx++)
                {
                    Fill(bmpAddress, bmpWidth, bmpHeight, (bx * blockW) + x, (by * blockH) + y, blockW, blockH, (bx + by) % 2 == 0 ? 0xFFBFBFBF : 0xFFFFFFFF);
                }
            }
        }
        public static unsafe void TransparencyGrid(uint* bmpAddress, int bmpWidth, int bmpHeight, int blockW, int blockH)
        {
            TransparencyGrid(bmpAddress, bmpWidth, bmpHeight, 0, 0, blockW, blockH, (bmpWidth / blockW) + (bmpWidth % blockW == 0 ? 0 : 1), (bmpHeight / blockH) + (bmpHeight % blockH == 0 ? 0 : 1));
        }

        public static unsafe void Fill(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, int height, uint color)
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

        public static unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, uint[][] colors, bool xFlip, bool yFlip)
        {
            int height = colors.Length;
            for (int cy = 0; cy < height; cy++)
            {
                int py = yFlip ? (y + (height - 1 - cy)) : (y + cy);
                if (py >= 0 && py < bmpHeight)
                {
                    uint[] arrY = colors[cy];
                    int width = arrY.Length;
                    for (int cx = 0; cx < width; cx++)
                    {
                        int px = xFlip ? (x + (width - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < bmpWidth)
                        {
                            DrawUnchecked(bmpAddress + px + (py * bmpWidth), arrY[cx]);
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
            if (color != 0x00000000)
            {
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
}
