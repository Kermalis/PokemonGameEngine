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
                    int width = ps.Width / spriteWidth;
                    int height = ps.Height / spriteHeight;
                    sprites = new uint[width * height][][];
                    int sprite = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            sprites[sprite] = new uint[spriteWidth][];
                            for (int px = 0; px < spriteWidth; px++)
                            {
                                sprites[sprite][px] = new uint[spriteHeight];
                                for (int py = 0; py < spriteHeight; py++)
                                {
                                    sprites[sprite][px][py] = *(bmpAddress + ((x * spriteWidth) + px) + (((y * spriteHeight) + py) * ps.Width));
                                }
                            }
                            sprite++;
                        }
                    }
                }
                return sprites;
            }
        }

        public static unsafe void Fill(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, int height, uint color)
        {
            for (int px = x; px < x + width; px++)
            {
                if (px >= 0 && px < bmpWidth)
                {
                    for (int py = x; py < y + height; py++)
                    {
                        if (py >= 0 && py < bmpHeight)
                        {
                            Draw(bmpAddress + px + (py * bmpWidth), color);
                        }
                    }
                }
            }
        }

        public static unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, uint[][] colors, bool xFlip, bool yFlip)
        {
            for (int px = 0; px < colors.Length; px++)
            {
                uint[] arr = colors[px];
                for (int py = 0; py < arr.Length; py++)
                {
                    int tx = xFlip ? (x + (colors.Length - 1 - px)) : (x + px);
                    if (tx >= 0 && tx < bmpWidth)
                    {
                        int ty = yFlip ? (y + (arr.Length - 1 - py)) : (y + py);
                        if (ty >= 0 && ty < bmpHeight)
                        {
                            Draw(bmpAddress + tx + (ty * bmpWidth), arr[py]);
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
