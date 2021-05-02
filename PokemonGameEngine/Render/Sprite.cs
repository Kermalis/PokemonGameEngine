using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render
{
    internal delegate void SpriteCallback(Sprite sprite);

    internal sealed class Sprite
    {
        public IImage Image;
        public int X;
        public int Y;
        public bool IsInvisible;
        public bool XFlip;
        public bool YFlip;

        public object Data;
        public SpriteCallback Callback;

        public static void DoCallbacks(IEnumerable<Sprite> sprites)
        {
            foreach (Sprite s in sprites)
            {
                s.Callback?.Invoke(s);
            }
        }
        public static unsafe void DrawAll(uint* bmpAddress, int bmpWidth, int bmpHeight, IEnumerable<Sprite> sprites)
        {
            foreach (Sprite s in sprites)
            {
                s.DrawOn(bmpAddress, bmpWidth, bmpHeight);
            }
        }

        public unsafe void DrawOn(uint* bmpAddress, int bmpWidth, int bmpHeight, int xOffset = 0, int yOffset = 0)
        {
            if (IsInvisible)
            {
                return;
            }

            fixed (uint* imgBmpAddress = Image.Bitmap)
            {
                RenderUtils.DrawBitmap(bmpAddress, bmpWidth, bmpHeight, X + xOffset, Y + yOffset, imgBmpAddress, Image.Width, Image.Height, xFlip: XFlip, yFlip: YFlip);
            }
        }
    }
}
