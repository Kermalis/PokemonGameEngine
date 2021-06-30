using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal delegate void SpriteCallback(Sprite sprite);
    internal unsafe delegate void SpriteDrawMethod(Sprite sprite, uint* dst, int dstW, int dstH, int xOffset = 0, int yOffset = 0);

    internal sealed class Sprite
    {
        public Sprite Next;
        public Sprite Prev;

        public IImage Image;
        public int Priority { get; init; }
        public int X;
        public int Y;
        public bool IsInvisible;
        public bool XFlip;
        public bool YFlip;

        public object Data;
        public SpriteDrawMethod DrawMethod;
        public SpriteCallback Callback;
        public SpriteCallback RCallback;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DrawOn(uint* dst, int dstW, int dstH, int xOffset = 0, int yOffset = 0)
        {
            DrawOn(this, dst, dstW, dstH, xOffset: xOffset, yOffset: yOffset);
        }
        public static unsafe void DrawOn(Sprite s, uint* dst, int dstW, int dstH, int xOffset = 0, int yOffset = 0)
        {
            if (s.IsInvisible)
            {
                return;
            }

            IImage img = s.Image;
            fixed (uint* src = img.Bitmap)
            {
                int srcW = img.Width;
                PixelSupplier pixSupply = Renderer.MakeBitmapSupplier(src, srcW);
                Renderer.DrawBitmap(dst, dstW, dstH, s.X + xOffset, s.Y + yOffset, pixSupply, srcW, img.Height, xFlip: s.XFlip, yFlip: s.YFlip);
            }
        }

        public void Dispose()
        {
            // Do not dispose next or prev so we can continue looping after this gets removed
            Data = null;
            DrawMethod = null;
            Callback = null;
            Image = null;
        }
    }

    internal sealed class SpriteList
    {
        public Sprite First;
        public int Count;

        public void Add(Sprite sprite)
        {
            if (First is null)
            {
                First = sprite;
                Count = 1;
                return;
            }
            Sprite s = First;
            while (true)
            {
                if (s.Priority > sprite.Priority)
                {
                    // The new sprite has a lower priority than s, so insert new before s
                    if (s == First)
                    {
                        First = sprite;
                    }
                    else
                    {
                        Sprite prev = s.Prev;
                        sprite.Prev = prev;
                        prev.Next = sprite;
                    }
                    s.Prev = sprite;
                    sprite.Next = s;
                    Count++;
                    return;
                }
                // Iterate to next sprite if there is one
                Sprite next = s.Next;
                if (next is null)
                {
                    // The new sprite is the highest priority or tied for it, so place new at the last position
                    s.Next = sprite;
                    sprite.Prev = s;
                    Count++;
                    return;
                }
                s = next;
            }
        }
        public void Remove(Sprite sprite)
        {
            if (sprite == First)
            {
                Sprite next = sprite.Next;
                if (next is not null)
                {
                    next.Prev = null;
                }
                First = next;
            }
            else
            {
                Sprite prev = sprite.Prev;
                Sprite next = sprite.Next;
                if (next is not null)
                {
                    next.Prev = prev;
                }
                prev.Next = next;
            }
            sprite.Dispose();
            Count--;
        }

        public void DoCallbacks()
        {
            for (Sprite s = First; s is not null; s = s.Next)
            {
                s.Callback?.Invoke(s);
            }
        }
        public void DoRCallbacks()
        {
            for (Sprite s = First; s is not null; s = s.Next)
            {
                s.RCallback?.Invoke(s);
            }
        }
        public unsafe void DrawAll(uint* dst, int dstW, int dstH)
        {
            for (Sprite s = First; s is not null; s = s.Next)
            {
                if (s.DrawMethod is not null)
                {
                    s.DrawMethod(s, dst, dstW, dstH);
                }
                else
                {
                    s.DrawOn(dst, dstW, dstH);
                }
            }
        }
    }
}
