using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        public static void OverwriteBitmap(uint* dst, int dstW, int dstH, int x, int y, PixelSupplier src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
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
                            *GetPixelAddress(dst, dstW, px, py) = src(cx, cy);
                        }
                    }
                }
            }
        }

        public static void DrawBitmap(uint* dst, int dstW, int dstH, int x, int y, PixelSupplier src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
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
                            DrawPoint_Unchecked(GetPixelAddress(dst, dstW, px, py), src(cx, cy));
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawBitmapSized(uint* dst, int dstW, int dstH, int x, int y, int width, int height, PixelSupplier src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
        {
            // Slight optimization
            if (width == srcW && height == srcH)
            {
                DrawBitmap(dst, dstW, dstH, x, y, src, srcW, srcH, xFlip: xFlip, yFlip: yFlip);
                return;
            }
            float wScale = (float)width / srcW;
            float hScale = (float)height / srcH;
            DrawBitmapSizedScaled(dst, dstW, dstH, x, y, width, height, wScale, hScale, src, xFlip: xFlip, yFlip: yFlip);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawBitmapScaled(uint* dst, int dstW, int dstH, int x, int y, float wScale, float hScale, PixelSupplier src, int srcW, int srcH, bool xFlip = false, bool yFlip = false)
        {
            // Slight optimization
            if (wScale == 1 && hScale == 1)
            {
                DrawBitmap(dst, dstW, dstH, x, y, src, srcW, srcH, xFlip: xFlip, yFlip: yFlip);
                return;
            }
            int width = (int)(srcW * wScale);
            int height = (int)(srcH * hScale);
            DrawBitmapSizedScaled(dst, dstW, dstH, x, y, width, height, wScale, hScale, src, xFlip: xFlip, yFlip: yFlip);
        }

        private static void DrawBitmapSizedScaled(uint* dst, int dstW, int dstH, int x, int y, int width, int height, float wScale, float hScale, PixelSupplier src, bool xFlip = false, bool yFlip = false)
        {
            for (int cy = 0; cy < height; cy++)
            {
                int py = yFlip ? (y + (height - 1 - cy)) : (y + cy);
                if (py >= 0 && py < dstH)
                {
                    int ty = (int)(cy / hScale);
                    for (int cx = 0; cx < width; cx++)
                    {
                        int px = xFlip ? (x + (width - 1 - cx)) : (x + cx);
                        if (px >= 0 && px < dstW)
                        {
                            int tx = (int)(cx / wScale);
                            DrawPoint_Unchecked(GetPixelAddress(dst, dstW, px, py), src(tx, ty));
                        }
                    }
                }
            }
        }
    }
}
