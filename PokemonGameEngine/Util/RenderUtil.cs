using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Kermalis.PokemonGameEngine.Util
{
    internal sealed class RenderUtil
    {
        public static unsafe uint[][][] LoadSpriteSheet(string resource, int spriteWidth, int spriteHeight)
        {
            var bmp = new Bitmap(Utils.GetResourceStream(resource));
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
                            uint[][] arrY = new uint[spriteWidth][];
                            for (int py = 0; py < spriteWidth; py++)
                            {
                                uint[] arrX = new uint[spriteHeight];
                                for (int px = 0; px < spriteHeight; px++)
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
                            Draw(bmpAddress + px + (py * bmpWidth), color);
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
                    uint[] arr = colors[cy];
                    int width = arr.Length;
                    for (int cx = 0; cx < width; cx++)
                    {
                        int px = xFlip ? (x + (width - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < bmpWidth)
                        {
                            Draw(bmpAddress + px + (py * bmpWidth), arr[cx]);
                        }
                    }
                }
            }
        }

        public static unsafe void Draw(uint* pixelAddress, uint color)
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
