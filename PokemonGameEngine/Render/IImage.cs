using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal interface IImage
    {
        uint[] Bitmap { get; }
        int Width { get; }
        int Height { get; }
    }
    internal static class ImageExtensions
    {
        public unsafe delegate void DrawMethod(uint* bmpAddress, int bmpWidth, int bmpHeight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Draw(this IImage img, DrawMethod drawMethod)
        {
            fixed (uint* bmpAddress = img.Bitmap)
            {
                drawMethod(bmpAddress, img.Width, img.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this IImage img, IImage otherImg, float x, float y)
        {
            fixed (uint* otherBmpAddress = otherImg.Bitmap)
            {
                img.DrawOn(otherBmpAddress, otherImg.Width, otherImg.Height, x, y);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this IImage img, IImage otherImg, int x, int y)
        {
            fixed (uint* otherBmpAddress = otherImg.Bitmap)
            {
                img.DrawOn(otherBmpAddress, otherImg.Width, otherImg.Height, x, y);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this IImage img, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, float x, float y)
        {
            fixed (uint* bmpAddress = img.Bitmap)
            {
                RenderUtils.DrawBitmap(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, bmpAddress, img.Width, img.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this IImage img, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, int x, int y)
        {
            fixed (uint* bmpAddress = img.Bitmap)
            {
                RenderUtils.DrawBitmap(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, bmpAddress, img.Width, img.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawSizedOn(this IImage img, IImage otherImg, float x, float y, float width, float height)
        {
            fixed (uint* otherBmpAddress = otherImg.Bitmap)
            {
                img.DrawSizedOn(otherBmpAddress, otherImg.Width, otherImg.Height, x, y, width, height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawSizedOn(this IImage img, IImage otherImg, int x, int y, int width, int height)
        {
            fixed (uint* otherBmpAddress = otherImg.Bitmap)
            {
                img.DrawSizedOn(otherBmpAddress, otherImg.Width, otherImg.Height, x, y, width, height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawSizedOn(this IImage img, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, float x, float y, float width, float height)
        {
            fixed (uint* bmpAddress = img.Bitmap)
            {
                RenderUtils.DrawBitmapSized(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, width, height, bmpAddress, img.Width, img.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawSizedOn(this IImage img, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, int x, int y, int width, int height)
        {
            fixed (uint* bmpAddress = img.Bitmap)
            {
                RenderUtils.DrawBitmapSized(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, width, height, bmpAddress, img.Width, img.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawScaledOn(this IImage img, IImage otherImg, float x, float y, float wScale, float hScale)
        {
            fixed (uint* otherBmpAddress = otherImg.Bitmap)
            {
                img.DrawScaledOn(otherBmpAddress, otherImg.Width, otherImg.Height, x, y, wScale, hScale);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawScaledOn(this IImage img, IImage otherImg, int x, int y, int wScale, int hScale)
        {
            fixed (uint* otherBmpAddress = otherImg.Bitmap)
            {
                img.DrawScaledOn(otherBmpAddress, otherImg.Width, otherImg.Height, x, y, wScale, hScale);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawScaledOn(this IImage img, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, float x, float y, float wScale, float hScale)
        {
            fixed (uint* bmpAddress = img.Bitmap)
            {
                RenderUtils.DrawBitmapScaled(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, wScale, hScale, bmpAddress, img.Width, img.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawScaledOn(this IImage img, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, int x, int y, float wScale, float hScale)
        {
            fixed (uint* bmpAddress = img.Bitmap)
            {
                RenderUtils.DrawBitmapScaled(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, wScale, hScale, bmpAddress, img.Width, img.Height);
            }
        }
    }
}
