﻿using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render
{
    internal delegate void SpriteCallback(Sprite sprite);

    internal sealed class Sprite
    {
        public Sprite Next;
        public Sprite Prev;

        public IImage Image;
        /// <summary>After this is updated, a call will need to be made to <see cref="SpriteList.SortByPriority"/>. Higher priorities are rendered last to appear above everything else</summary>
        public int Priority;
        public Vec2I Pos;
        public bool IsInvisible;
        public bool XFlip;
        public bool YFlip;

        public object Data;
        public object Tag;
        public SpriteCallback Callback;

        public void Render(Vec2I translation = default)
        {
            if (IsInvisible)
            {
                return;
            }

            IImage img = Image;
            GUIRenderer.Texture(img.Texture, Rect.FromSize(Pos + translation, img.Size), new UV(XFlip, YFlip));
        }

        public void Dispose()
        {
            // Do not dispose next or prev so we can continue looping after this gets removed
            Data = null;
            Callback = null;
            Image?.DeductReference();
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
                        _first = sprite; // new is now the first sprite
                    }
                    else
                    {
                        Sprite prev = s.Prev; // Connect the one before and one after with the new sprite
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
                if (next is not null) // This was not the only sprite
                {
                    next.Prev = null; // Make the next one the first one
                }
                _first = next;
            }
            else // Not the first one so we have a previous one
            {
                Sprite prev = sprite.Prev;
                Sprite next = sprite.Next;
                if (next is not null) // This was not last
                {
                    next.Prev = prev; // Connect the previous and next together
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
        public void DrawAll()
        {
            for (Sprite s = _first; s is not null; s = s.Next)
            {
                s.Render();
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
