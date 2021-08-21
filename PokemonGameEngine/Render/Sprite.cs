using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.Images;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render
{
    internal delegate void SpriteCallback(Sprite sprite);
    internal delegate void SpriteDrawMethod(Sprite sprite, Pos2D translation = default);

    internal class Sprite
    {
        public Sprite Next;
        public Sprite Prev;

        public IImage Image;
        /// <summary>After this is updated, a call will need to be made to <see cref="SpriteList.SortByPriority"/>. Higher priorities are rendered last</summary>
        public virtual int Priority { get; set; }
        public Pos2D Pos;
        public bool IsInvisible;
        public bool XFlip;
        public bool YFlip;

        public object Data;
        public object Tag;
        public SpriteDrawMethod DrawMethod;
        public SpriteCallback Callback;
        public SpriteCallback RCallback;

        public void Render(Pos2D translation = default)
        {
            if (IsInvisible)
            {
                return;
            }

            IImage img = Image;
            GUIRenderer.Instance.RenderTexture(img.Texture, new Rect2D(Pos + translation, img.Size), xFlip: XFlip, yFlip: YFlip);
        }

        public void Dispose()
        {
            // Do not dispose next or prev so we can continue looping after this gets removed
            Data = null;
            DrawMethod = null;
            Callback = null;
            Image?.DeductReference(Game.OpenGL);
            Image = null;
        }
    }

    internal sealed class SpriteList : IEnumerable<Sprite>
    {
        private Sprite _first;
        public int Count { get; private set; }

        public void Add(Sprite sprite)
        {
            if (_first is null)
            {
                _first = sprite;
                Count = 1;
                return;
            }
            Sprite s = _first;
            while (true)
            {
                if (s.Priority > sprite.Priority)
                {
                    // The new sprite has a lower priority than s, so insert new before s
                    if (s == _first)
                    {
                        _first = sprite;
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
        public void RemoveAndDispose(Sprite sprite)
        {
            if (sprite == _first)
            {
                Sprite next = sprite.Next;
                if (next is not null)
                {
                    next.Prev = null;
                }
                _first = next;
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

        public Sprite FirstWithTagOrDefault(object tag)
        {
            for (Sprite s = _first; s is not null; s = s.Next)
            {
                if (s.Tag?.Equals(tag) == true)
                {
                    return s;
                }
            }
            return null;
        }

        public void SortByPriority()
        {
            if (_first is null)
            {
                return;
            }
            for (Sprite s = _first.Next; s is not null; s = s.Next)
            {
                Sprite cur = s;
                // Search all values before the current item
                // If a value below has a a higher priority, shift it up
                Sprite prev = cur.Prev;
                while (prev.Priority > s.Priority)
                {
                    Sprite prev2 = prev.Prev;
                    Sprite next = cur.Next;
                    prev.Next = next;
                    prev.Prev = cur;
                    cur.Next = prev;
                    cur.Prev = prev2;
                    if (next is not null)
                    {
                        next.Prev = prev;
                    }
                    if (prev2 is null)
                    {
                        _first = cur;
                        break;
                    }
                    prev2.Next = cur;
                    prev = prev2;
                }
            }
        }

        public void DoCallbacks()
        {
            for (Sprite s = _first; s is not null; s = s.Next)
            {
                s.Callback?.Invoke(s);
            }
        }
        public void DoRCallbacks()
        {
            for (Sprite s = _first; s is not null; s = s.Next)
            {
                s.RCallback?.Invoke(s);
            }
        }
        public void DrawAll()
        {
            for (Sprite s = _first; s is not null; s = s.Next)
            {
                if (s.DrawMethod is not null)
                {
                    s.DrawMethod(s);
                }
                else
                {
                    s.Render();
                }
            }
        }

        public IEnumerator<Sprite> GetEnumerator()
        {
            for (Sprite s = _first; s is not null; s = s.Next)
            {
                yield return s;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Sprite>)this).GetEnumerator();
        }
    }
}
