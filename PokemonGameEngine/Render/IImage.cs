using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal unsafe delegate void DrawMethod(uint* dst, int dstW, int dstH);

    internal interface IImage
    {
        uint[] Bitmap { get; }
        int Width { get; }
        int Height { get; }
    }

    internal static class ImageExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Draw(this IImage dstImg, DrawMethod drawMethod)
        {
            fixed (uint* dst = dstImg.Bitmap)
            {
                drawMethod(dst, dstImg.Width, dstImg.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this IImage srcImg, IImage dstImg, float x, float y)
        {
            fixed (uint* dst = dstImg.Bitmap)
            {
                srcImg.DrawOn(dst, dstImg.Width, dstImg.Height, x, y);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this IImage srcImg, IImage dstImg, int x, int y)
        {
            fixed (uint* dst = dstImg.Bitmap)
            {
                srcImg.DrawOn(dst, dstImg.Width, dstImg.Height, x, y);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this IImage srcImg, uint* dst, int dstW, int dstH, float x, float y)
        {
            fixed (uint* src = srcImg.Bitmap)
            {
                Renderer.DrawBitmap(dst, dstW, dstH, x, y, src, srcImg.Width, srcImg.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this IImage srcImg, uint* dst, int dstW, int dstH, int x, int y)
        {
            fixed (uint* src = srcImg.Bitmap)
            {
                Renderer.DrawBitmap(dst, dstW, dstH, x, y, src, srcImg.Width, srcImg.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawSizedOn(this IImage srcImg, IImage dstImg, float x, float y, float width, float height)
        {
            fixed (uint* dst = dstImg.Bitmap)
            {
                srcImg.DrawSizedOn(dst, dstImg.Width, dstImg.Height, x, y, width, height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawSizedOn(this IImage srcImg, IImage dstImg, int x, int y, int width, int height)
        {
            fixed (uint* dst = dstImg.Bitmap)
            {
                srcImg.DrawSizedOn(dst, dstImg.Width, dstImg.Height, x, y, width, height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawSizedOn(this IImage srcImg, uint* dst, int dstW, int dstH, float x, float y, float width, float height)
        {
            fixed (uint* src = srcImg.Bitmap)
            {
                Renderer.DrawBitmapSized(dst, dstW, dstH, x, y, width, height, src, srcImg.Width, srcImg.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawSizedOn(this IImage srcImg, uint* dst, int dstW, int dstH, int x, int y, int width, int height)
        {
            fixed (uint* src = srcImg.Bitmap)
            {
                Renderer.DrawBitmapSized(dst, dstW, dstH, x, y, width, height, src, srcImg.Width, srcImg.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawScaledOn(this IImage srcImg, IImage dstImg, float x, float y, float wScale, float hScale)
        {
            fixed (uint* dst = dstImg.Bitmap)
            {
                srcImg.DrawScaledOn(dst, dstImg.Width, dstImg.Height, x, y, wScale, hScale);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawScaledOn(this IImage srcImg, IImage dstImg, int x, int y, int wScale, int hScale)
        {
            fixed (uint* dst = dstImg.Bitmap)
            {
                srcImg.DrawScaledOn(dst, dstImg.Width, dstImg.Height, x, y, wScale, hScale);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawScaledOn(this IImage srcImg, uint* dst, int dstW, int dstH, float x, float y, float wScale, float hScale)
        {
            fixed (uint* src = srcImg.Bitmap)
            {
                Renderer.DrawBitmapScaled(dst, dstW, dstH, x, y, wScale, hScale, src, srcImg.Width, srcImg.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawScaledOn(this IImage srcImg, uint* dst, int dstW, int dstH, int x, int y, float wScale, float hScale)
        {
            fixed (uint* src = srcImg.Bitmap)
            {
                Renderer.DrawBitmapScaled(dst, dstW, dstH, x, y, wScale, hScale, src, srcImg.Width, srcImg.Height);
            }
        }
    }
}
