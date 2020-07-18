using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.PokemonGameEngine.GUI;

namespace Kermalis.PokemonGameEngine.Util
{
    internal sealed class RenderUtil
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
        public static unsafe uint[][] ToColors(WriteableBitmap wb)
        {
            using (ILockedFramebuffer l = wb.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                PixelSize ps = wb.PixelSize;
                int height = ps.Height;
                int width = ps.Width;
                uint[][] arrY = new uint[height][];
                for (int py = 0; py < height; py++)
                {
                    uint[] arrX = new uint[width];
                    for (int px = 0; px < width; px++)
                    {
                        arrX[px] = *(bmpAddress + px + (py * width));
                    }
                    arrY[py] = arrX;
                }
                return arrY;
            }
        }

        public static unsafe uint[][] LoadSprite(string resource)
        {
            using (WriteableBitmap wb = ToWriteableBitmap(new Bitmap(Utils.GetResourceStream(resource))))
            {
                return ToColors(wb);
            }
        }

        public static unsafe uint[][][] LoadSpriteSheet(string resource, int spriteWidth, int spriteHeight)
        {
            using (WriteableBitmap wb = ToWriteableBitmap(new Bitmap(Utils.GetResourceStream(resource))))
            using (ILockedFramebuffer l = wb.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                PixelSize ps = wb.PixelSize;
                int numSpritesX = ps.Width / spriteWidth;
                int numSpritesY = ps.Height / spriteHeight;
                uint[][][] sprites = new uint[numSpritesX * numSpritesY][][];
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
                return sprites;
            }
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

        public static unsafe void DrawBitmap(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, WriteableBitmap wb)
        {
            using (ILockedFramebuffer l = wb.Lock())
            {
                uint* wbAddress = (uint*)l.Address.ToPointer();
                PixelSize ps = wb.PixelSize;
                int height = ps.Height;
                int width = ps.Width;
                for (int cy = 0; cy < height; cy++)
                {
                    int py = y + cy;
                    if (py >= 0 && py < bmpHeight)
                    {
                        for (int cx = 0; cx < width; cx++)
                        {
                            int px = x + cx;
                            if (px >= 0 && px < bmpWidth)
                            {
                                DrawUnchecked(bmpAddress + px + (py * bmpWidth), *(wbAddress + cx + (cy * width)));
                            }
                        }
                    }
                }
            }
        }

        public static unsafe void DrawImage(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, uint[][] colors, bool xFlip, bool yFlip)
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

        public static unsafe void DrawLine(uint* bmpAddress, int bmpWidth, int bmpHeight, int x1, int y1, int x2, int y2, uint color)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            for (int px = x1; px < x2; px++)
            {
                if (px >= 0 && px < bmpWidth)
                {
                    int py = y1 + dy * (px - x1) / dx;
                    if (py >= 0 && py < bmpHeight)
                    {
                        DrawUnchecked(bmpAddress + px + (py * bmpWidth), color);
                    }
                }
            }
        }

        public static unsafe void DrawStretchedImage(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, int height, uint[][] colors)
        {
            int cHeight = colors.Length;
            float hScale = (float)height / cHeight;
            for (int cy = 0; cy < height; cy++)
            {
                int py = y + cy;
                if (py >= 0 && py < bmpHeight)
                {
                    uint[] arrY = colors[(int)(cy / hScale)];
                    int cWidth = arrY.Length;
                    float wScale = (float)width / cWidth;
                    for (int cx = 0; cx < width; cx++)
                    {
                        int px = x + cx;
                        if (px >= 0 && px < bmpWidth)
                        {
                            DrawUnchecked(bmpAddress + px + (py * bmpWidth), arrY[(int)(cx / wScale)]);
                        }
                    }
                }
            }
        }

        public static unsafe void DrawString(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, string str, Font font, uint[] fontColors)
        {
            int index = 0;
            int nextXOffset = 0;
            int nextYOffset = 0;
            while (index < str.Length)
            {
                int xOffset = x + nextXOffset;
                int yOffset = y + nextYOffset;
                Font.Glyph glyph = font.GetGlyph(str, ref index, ref nextXOffset, ref nextYOffset);
                if (glyph != null)
                {
                    int curBit = 0;
                    int curByte = 0;
                    for (int py = yOffset; py < yOffset + font.FontHeight; py++)
                    {
                        for (int px = xOffset; px < xOffset + glyph.CharWidth; px++)
                        {
                            if (py >= 0 && py < bmpHeight && px >= 0 && px < bmpWidth)
                            {
                                DrawUnchecked(bmpAddress + px + (py * bmpWidth), fontColors[(glyph.Bitmap[curByte] >> (8 - font.BitsPerPixel - curBit)) % (1 << font.BitsPerPixel)]);
                            }
                            curBit = (curBit + font.BitsPerPixel) % 8;
                            if (curBit == 0)
                            {
                                curByte++;
                            }
                        }
                    }
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
