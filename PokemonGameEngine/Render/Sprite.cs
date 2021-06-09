namespace Kermalis.PokemonGameEngine.Render
{
    internal delegate void SpriteCallback(Sprite sprite);

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
        public SpriteCallback Callback;

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

        public void Dispose()
        {
            Data = null;
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
                    // Found a task with a higher priority, so insert the new one before it
                    if (s != First)
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
                // Iterate to next task if there is one
                Sprite next = s.Next;
                if (next is null)
                {
                    // The new task is the highest priority or tied for it, so it gets placed at the last position
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
        public unsafe void DrawAll(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            for (Sprite s = First; s is not null; s = s.Next)
            {
                s.DrawOn(bmpAddress, bmpWidth, bmpHeight);
            }
        }
    }
}
