using Kermalis.PokemonGameEngine.Util;
using SDL2;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class RenderUtils
    {
        private static unsafe IntPtr ConvertSurfaceFormat(IntPtr surface)
        {
            IntPtr result = surface;
            var surPtr = (SDL.SDL_Surface*)surface;
            var pixelFormatPtr = (SDL.SDL_PixelFormat*)surPtr->format;
            if (pixelFormatPtr->format != SDL.SDL_PIXELFORMAT_ABGR8888)
            {
                result = SDL.SDL_ConvertSurfaceFormat(surface, SDL.SDL_PIXELFORMAT_ABGR8888, 0);
                SDL.SDL_FreeSurface(surface);
            }
            return result;
        }
        private static unsafe IntPtr GetSurfacePixels(IntPtr surface)
        {
            return ((SDL.SDL_Surface*)surface)->pixels;
        }
        private static unsafe int GetSurfaceWidth(IntPtr surface)
        {
            return ((SDL.SDL_Surface*)surface)->w;
        }
        private static unsafe int GetSurfaceHeight(IntPtr surface)
        {
            return ((SDL.SDL_Surface*)surface)->h;
        }
        private static unsafe IntPtr StreamToRWops(Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            fixed (byte* b = bytes)
            {
                return SDL.SDL_RWFromMem(new IntPtr(b), bytes.Length);
            }
        }
        public static unsafe void GetTextureData(Stream stream, out int width, out int height, out uint[] bitmap)
        {
            IntPtr rwops = StreamToRWops(stream);
            IntPtr surface = SDL_image.IMG_Load_RW(rwops, 1);
            surface = ConvertSurfaceFormat(surface);
            width = GetSurfaceWidth(surface);
            height = GetSurfaceHeight(surface);
            bitmap = new uint[width * height];
            fixed (uint* b = bitmap)
            {
                int len = width * height * sizeof(uint);
                //Buffer.MemoryCopy(GetSurfacePixels(surface).ToPointer(), b, len, len);
                SDL.SDL_memcpy(new IntPtr(b), GetSurfacePixels(surface), new IntPtr(len));
            }
        }
        // Currently unused
        public static unsafe IntPtr LoadTexture_PNG(IntPtr renderer, string resource)
        {
            using (Stream stream = Utils.GetResourceStream(resource))
            {
                return SDL_image.IMG_LoadTextureTyped_RW(renderer, StreamToRWops(stream), 1, "PNG");
            }
        }

        public static unsafe Sprite[] LoadSpriteSheet(string resource, int spriteWidth, int spriteHeight)
        {
            uint[][] bitmaps = LoadBitmapSheet(resource, spriteWidth, spriteHeight);
            var arr = new Sprite[bitmaps.Length];
            for (int i = 0; i < bitmaps.Length; i++)
            {
                arr[i] = new Sprite(bitmaps[i], spriteWidth, spriteHeight);
            }
            return arr;
        }
        public static unsafe uint[][] LoadBitmapSheet(string resource, int spriteWidth, int spriteHeight)
        {
            using (Stream stream = Utils.GetResourceStream(resource))
            {
                GetTextureData(stream, out int sheetWidth, out int sheetHeight, out uint[] pixels);
                fixed (uint* bmpAddress = pixels)
                {
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

        /// <summary>Returns true if any pixel is inside of the target bitmap.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInsideBitmap(int bmpWidth, int bmpHeight, int x, int y, int w, int h)
        {
            return x < bmpWidth && x + w > 0 && y < bmpHeight && y + h > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void FillColor_Overwrite(uint* bmpAddress, int bmpWidth, int bmpHeight, uint color)
        {
            FillColor_Overwrite(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, color);
        }
        public static unsafe void FillColor_Overwrite(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, int height, uint color)
        {
            // No line overwriting yet
            for (int py = y; py < y + height; py++)
            {
                if (py >= 0 && py < bmpHeight)
                {
                    for (int px = x; px < x + width; px++)
                    {
                        if (px >= 0 && px < bmpWidth)
                        {
                            *GetPixelAddress(bmpAddress, bmpWidth, px, py) = color;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void FillColor(uint* bmpAddress, int bmpWidth, int bmpHeight, uint color)
        {
            FillColor(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, color);
        }
        public static unsafe void FillColor(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, int height, uint color)
        {
            if (height == 1)
            {
                DrawHorizontalLine(bmpAddress, bmpWidth, bmpHeight, x, y, width, color);
                return;
            }
            if (width == 1)
            {
                DrawVerticalLine(bmpAddress, bmpWidth, bmpHeight, x, y, height, color);
                return;
            }
            for (int py = y; py < y + height; py++)
            {
                if (py >= 0 && py < bmpHeight)
                {
                    for (int px = x; px < x + width; px++)
                    {
                        if (px >= 0 && px < bmpWidth)
                        {
                            DrawUnchecked(bmpAddress, bmpWidth, px, py, color);
                        }
                    }
                }
            }
        }
        public static unsafe void Modulate(uint* bmpAddress, int bmpWidth, int bmpHeight, float rMod, float gMod, float bMod, float aMod)
        {
            for (int y = 0; y < bmpHeight; y++)
            {
                for (int x = 0; x < bmpWidth; x++)
                {
                    ModulateUnchecked(GetPixelAddress(bmpAddress, bmpWidth, x, y), rMod, gMod, bMod, aMod);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                            DrawUnchecked(bmpAddress, bmpWidth, px, py, *GetPixelAddress(otherBmpAddress, otherBmpWidth, cx, cy));
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawBitmap(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, int height, uint[] otherBmp, int otherBmpWidth, int otherBmpHeight, bool xFlip = false, bool yFlip = false)
        {
            fixed (uint* otherBmpAddress = otherBmp)
            {
                DrawBitmap(bmpAddress, bmpWidth, bmpHeight, x, y, width, height, otherBmpAddress, otherBmpWidth, otherBmpHeight, xFlip: xFlip, yFlip: yFlip);
            }
        }
        public static unsafe void DrawBitmap(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int width, int height, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, bool xFlip = false, bool yFlip = false)
        {
            // Slight optimization
            if (width == otherBmpWidth && height == otherBmpHeight)
            {
                DrawBitmap(bmpAddress, bmpWidth, bmpHeight, x, y, otherBmpAddress, otherBmpWidth, otherBmpHeight, xFlip: xFlip, yFlip: yFlip);
                return;
            }
            float hScale = (float)height / otherBmpHeight;
            float wScale = (float)width / otherBmpWidth;
            for (int cy = 0; cy < height; cy++)
            {
                int py = yFlip ? (y + (height - 1 - cy)) : (y + cy);
                if (py >= 0 && py < bmpHeight)
                {
                    int ty = (int)(cy / hScale);
                    for (int cx = 0; cx < width; cx++)
                    {
                        int px = xFlip ? (x + (width - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < bmpWidth)
                        {
                            int tx = (int)(cx / wScale);
                            DrawUnchecked(bmpAddress, bmpWidth, px, py, *GetPixelAddress(otherBmpAddress, otherBmpWidth, tx, ty));
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
                    DrawUnchecked(bmpAddress, bmpWidth, px, y, color);
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
                    DrawUnchecked(bmpAddress, bmpWidth, x, py, color);
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
                DrawChecked(bmpAddress, bmpWidth, bmpHeight, px, py, color);
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
                DrawChecked(bmpAddress, bmpWidth, bmpHeight, px, py, color);
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

        // http://enchantia.com/graphapp/doc/tech/ellipses.html
        /// <summary>Works based on a center point and with radii. Even radii do not work properly.</summary>
        public static unsafe void FillEllipse_Center(uint* bmpAddress, int bmpWidth, int bmpHeight, int xCenter, int yCenter, int xRadius, int yRadius, uint color)
        {
            int x = 0,
                y = yRadius;
            int rx = x,
                ry = y;
            int width = 1;
            int height = 1;
            long a2 = Math.BigMul(xRadius, xRadius),
                b2 = Math.BigMul(yRadius, yRadius);
            long crit1 = -((a2 / 4) + (xRadius % 2) + b2);
            long crit2 = -((b2 / 4) + (yRadius % 2) + a2);
            long crit3 = -((b2 / 4) + (yRadius % 2));
            long t = -a2 * y;
            long dxt = 2 * b2 * x,
                dyt = -2 * a2 * y;
            long d2xt = 2 * b2,
                d2yt = 2 * a2;

            if (yRadius == 0)
            {
                FillColor(bmpAddress, bmpWidth, bmpHeight, xCenter - xRadius, yCenter, (2 * xRadius) + 1, 1, color);
                return;
            }

            void incx()
            {
                x++;
                dxt += d2xt;
                t += dxt;
            }
            void incy()
            {
                y--;
                dyt += d2yt;
                t += dyt;
            }

            while (y >= 0 && x <= xRadius)
            {
                if (t + (b2 * x) <= crit1 || t + (a2 * y) <= crit3)
                {
                    if (height == 1)
                    {
                        // Draw nothing
                    }
                    else if ((ry * 2) + 1 > (height - 1) * 2)
                    {
                        FillColor(bmpAddress, bmpWidth, bmpHeight, xCenter - rx, yCenter - ry, width, height - 1, color);
                        FillColor(bmpAddress, bmpWidth, bmpHeight, xCenter - rx, yCenter + ry + 1, width, 1 - height, color);
                        ry -= height - 1;
                        height = 1;
                    }
                    else
                    {
                        FillColor(bmpAddress, bmpWidth, bmpHeight, xCenter - rx, yCenter - ry, width, (ry * 2) + 1, color);
                        ry -= ry;
                        height = 1;
                    }
                    incx();
                    rx++;
                    width += 2;
                }
                else if (t - (a2 * y) > crit2)
                {
                    incy();
                    height++;
                }
                else
                {
                    if ((ry * 2) + 1 > height * 2)
                    {
                        FillColor(bmpAddress, bmpWidth, bmpHeight, xCenter - rx, yCenter - ry, width, height, color);
                        FillColor(bmpAddress, bmpWidth, bmpHeight, xCenter - rx, yCenter + ry + 1 - height, width, height, color);
                    }
                    else
                    {
                        FillColor(bmpAddress, bmpWidth, bmpHeight, xCenter - rx, yCenter - ry, width, (ry * 2) + 1, color);
                    }
                    incx();
                    incy();
                    rx++;
                    width += 2;
                    ry -= height;
                    height = 1;
                }
            }

            if (ry > height)
            {
                FillColor(bmpAddress, bmpWidth, bmpHeight, xCenter - rx, yCenter - ry, width, height, color);
                FillColor(bmpAddress, bmpWidth, bmpHeight, xCenter - rx, yCenter + ry + 1 - height, width, height, color);
            }
            else
            {
                FillColor(bmpAddress, bmpWidth, bmpHeight, xCenter - rx, yCenter - ry, width, (ry * 2) + 1, color);
            }
        }
        // https://stackoverflow.com/questions/2914807/plot-ellipse-from-rectangle
        /// <summary>Works based on a top-left point and with width and height. Even widths and heights work properly.</summary>
        public static unsafe void DrawEllipse_XY(uint* bmpAddress, int bmpWidth, int bmpHeight, int x0, int y0, int x1, int y1, bool fill, uint color)
        {
            void DrawRow(int xLeft, int xRight, int py)
            {
                DrawHorizontalLine(bmpAddress, bmpWidth, bmpHeight, xLeft, py, xRight - xLeft + 1, color);
            }
            int xb, yb, xc, yc;

            // Calculate height
            yb = yc = (y0 + y1) / 2;
            int qb = (y0 < y1) ? (y1 - y0) : (y0 - y1);
            int qy = qb;
            int dy = qb / 2;
            if (qb % 2 != 0)
            {
                // Bounding box has even pixel height
                yc++;
            }

            // Calculate width
            xb = xc = (x0 + x1) / 2;
            int qa = (x0 < x1) ? (x1 - x0) : (x0 - x1);
            int qx = qa % 2;
            int dx = 0;
            long qt = (long)qa*qa + (long)qb*qb -2L*qa*qa*qb;
            if (qx != 0)
            {
                // Bounding box has even pixel width
                xc++;
                qt += 3L * qb * qb;
            }

            // Start at (dx, dy) = (0, b) and iterate until (a, 0) is reached
            while (qy >= 0 && qx <= qa)
            {
                // Draw the new points
                if (!fill)
                {
                    DrawChecked(bmpAddress, bmpWidth, bmpHeight, xb - dx, yb - dy, color);
                    if (dx != 0 || xb != xc)
                    {
                        DrawChecked(bmpAddress, bmpWidth, bmpHeight, xc + dx, yb - dy, color);
                        if (dy != 0 || yb != yc)
                        {
                            DrawChecked(bmpAddress, bmpWidth, bmpHeight, xc + dx, yc + dy, color);
                        }
                    }
                    if (dy != 0 || yb != yc)
                    {
                        DrawChecked(bmpAddress, bmpWidth, bmpHeight, xb - dx, yc + dy, color);
                    }
                }

                // If a (+1, 0) step stays inside the ellipse, do it
                if (qt + 2L * qb * qb * qx + 3L * qb * qb <= 0L ||
                    qt + 2L * qa * qa * qy - (long)qa * qa <= 0L)
                {
                    qt += 8L * qb * qb + 4L * qb * qb * qx;
                    dx++;
                    qx += 2;
                    // If a (0, -1) step stays outside the ellipse, do it
                }
                else if (qt - 2L * qa * qa * qy + 3L * qa * qa > 0L)
                {
                    if (fill)
                    {
                        DrawRow(xb - dx, xc + dx, yc + dy);
                        if (dy != 0 || yb != yc)
                        {
                            DrawRow(xb - dx, xc + dx, yb - dy);
                        }
                    }
                    qt += 8L * qa * qa - 4L * qa * qa * qy;
                    dy--;
                    qy -= 2;
                    // Else step (+1, -1)
                }
                else
                {
                    if (fill)
                    {
                        DrawRow(xb - dx, xc + dx, yc + dy);
                        if (dy != 0 || yb != yc)
                        {
                            DrawRow(xb - dx, xc + dx, yb - dy);
                        }
                    }
                    qt += 8L * qb * qb + 4L * qb * qb * qx + 8L * qa * qa - 4L * qa * qa * qy;
                    dx++;
                    qx += 2;
                    dy--;
                    qy -= 2;
                }
            }   // End of while loop
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe uint* GetPixelAddress(uint* bmpAddress, int bmpWidth, int x, int y)
        {
            return bmpAddress + x + (y * bmpWidth);
        }

        // Colors must be RGBA8888 (0xAABBCCDD - AA is A, BB is B, CC is G, DD is R)
        public static unsafe void ModulateUnchecked(uint* pixelAddress, float rMod, float gMod, float bMod, float aMod)
        {
            uint current = *pixelAddress;
            uint r = GetR(current);
            uint g = GetG(current);
            uint b = GetB(current);
            uint a = GetA(current);
            r = (byte)(r * rMod);
            g = (byte)(g * gMod);
            b = (byte)(b * bMod);
            a = (byte)(a * aMod);
            *pixelAddress = Color(r, g, b, a);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawUnchecked(uint* bmpAddress, int bmpWidth, int x, int y, uint color)
        {
            DrawUnchecked(GetPixelAddress(bmpAddress, bmpWidth, x, y), color);
        }
        public static unsafe void DrawUnchecked(uint* pixelAddress, uint color)
        {
            uint aIn = GetA(color);
            if (aIn == 0)
            {
                return; // Fully transparent
            }
            else if (aIn == 0xFF)
            {
                *pixelAddress = color; // Fully opaque
            }
            else
            {
                uint rIn = GetR(color);
                uint gIn = GetG(color);
                uint bIn = GetB(color);
                uint current = *pixelAddress;
                uint rOld = GetR(current);
                uint gOld = GetG(current);
                uint bOld = GetB(current);
                uint aOld = GetA(current);
                uint r = (rIn * aIn / 0xFF) + (rOld * aOld * (0xFF - aIn) / (0xFF * 0xFF));
                uint g = (gIn * aIn / 0xFF) + (gOld * aOld * (0xFF - aIn) / (0xFF * 0xFF));
                uint b = (bIn * aIn / 0xFF) + (bOld * aOld * (0xFF - aIn) / (0xFF * 0xFF));
                uint a = aIn + (aOld * (0xFF - aIn) / 0xFF);
                *pixelAddress = Color(r, g, b, a);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawChecked(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, uint color)
        {
            if (y >= 0 && y < bmpHeight && x >= 0 && x < bmpWidth)
            {
                DrawUnchecked(bmpAddress, bmpWidth, x, y, color);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Color(uint r, uint g, uint b, uint a)
        {
            return (a << 24) | (b << 16) | (g << 8) | r;
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
    }
}
