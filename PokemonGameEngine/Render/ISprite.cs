using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal interface ISprite
    {
        uint[] Bitmap { get; }
        int Width { get; }
        int Height { get; }
    }
    internal static class SpriteExtensions
    {
        public unsafe delegate void DrawMethod(uint* bmpAddress, int bmpWidth, int bmpHeight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Draw(this ISprite sprite, DrawMethod drawMethod)
        {
            fixed (uint* bmpAddress = sprite.Bitmap)
            {
                drawMethod(bmpAddress, sprite.Width, sprite.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this ISprite sprite, ISprite otherSprite, float x, float y)
        {
            fixed (uint* otherBmpAddress = otherSprite.Bitmap)
            {
                sprite.DrawOn(otherBmpAddress, otherSprite.Width, otherSprite.Height, x, y);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this ISprite sprite, ISprite otherSprite, int x, int y)
        {
            fixed (uint* otherBmpAddress = otherSprite.Bitmap)
            {
                sprite.DrawOn(otherBmpAddress, otherSprite.Width, otherSprite.Height, x, y);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this ISprite sprite, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, float x, float y)
        {
            fixed (uint* bmpAddress = sprite.Bitmap)
            {
                RenderUtils.DrawBitmap(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, bmpAddress, sprite.Width, sprite.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this ISprite sprite, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, int x, int y)
        {
            fixed (uint* bmpAddress = sprite.Bitmap)
            {
                RenderUtils.DrawBitmap(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, bmpAddress, sprite.Width, sprite.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this ISprite sprite, ISprite otherSprite, float x, float y, float width, float height)
        {
            fixed (uint* otherBmpAddress = otherSprite.Bitmap)
            {
                sprite.DrawOn(otherBmpAddress, otherSprite.Width, otherSprite.Height, x, y, width, height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this ISprite sprite, ISprite otherSprite, int x, int y, int width, int height)
        {
            fixed (uint* otherBmpAddress = otherSprite.Bitmap)
            {
                sprite.DrawOn(otherBmpAddress, otherSprite.Width, otherSprite.Height, x, y, width, height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this ISprite sprite, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, float x, float y, float width, float height)
        {
            fixed (uint* bmpAddress = sprite.Bitmap)
            {
                RenderUtils.DrawBitmap(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, width, height, bmpAddress, sprite.Width, sprite.Height);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawOn(this ISprite sprite, uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, int x, int y, int width, int height)
        {
            fixed (uint* bmpAddress = sprite.Bitmap)
            {
                RenderUtils.DrawBitmap(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, width, height, bmpAddress, sprite.Width, sprite.Height);
            }
        }
    }
}
