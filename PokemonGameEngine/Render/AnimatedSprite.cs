using Kermalis.PokemonGameEngine.Render.Gif;
using Kermalis.SimpleGIF;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class AnimatedSprite : ISprite
    {
        public sealed class Frame
        {
            public uint[] Bitmap { get; }
            /// <summary>Delay in milliseconds</summary>
            public int Delay { get; }

            public Frame(DecodedGIF.Frame frame)
            {
                Bitmap = (uint[])frame.Bitmap.Clone();
                Delay = frame.Delay;
            }
        }
        public sealed class Sprite
        {
            public Frame[] Frames { get; }
            public int Width { get; }
            public int Height { get; }

            public Sprite(DecodedGIF gif)
            {
                Frames = new Frame[gif.Frames.Count];
                for (int i = 0; i < gif.Frames.Count; i++)
                {
                    Frames[i] = new Frame(gif.Frames[i]);
                }
                Width = gif.Width;
                Height = gif.Height;
            }
        }

        private readonly Sprite _sprite;
        public double SpeedModifier { get; set; } = 1d;

        public uint[] Bitmap => _sprite.Frames[_curFrameIndex].Bitmap;
        public int Width => _sprite.Width;
        public int Height => _sprite.Height;
        private int _curFrameIndex;
        private TimeSpan _nextFrameTime;

        public AnimatedSprite(string resource)
        {
            _sprite = LoadOrGet(resource);
            int frameDelay = _sprite.Frames[0].Delay;
            if (frameDelay != -1)
            {
                _nextFrameTime = TimeSpan.FromMilliseconds(frameDelay);
            }

            for (int i = 0; i < _loadedAnimSprites.Count; i++)
            {
                WeakReference<AnimatedSprite> w = _loadedAnimSprites[i];
                if (!w.TryGetTarget(out _))
                {
                    w.SetTarget(this);
                    return;
                }
            }
            _loadedAnimSprites.Add(new WeakReference<AnimatedSprite>(this));
        }

        private static readonly List<WeakReference<AnimatedSprite>> _loadedAnimSprites = new List<WeakReference<AnimatedSprite>>();
        private static readonly Dictionary<string, WeakReference<Sprite>> _loadedSprites = new Dictionary<string, WeakReference<Sprite>>();
        private static Sprite LoadOrGet(string resource)
        {
            Sprite s;
            if (!_loadedSprites.TryGetValue(resource, out WeakReference<Sprite> w))
            {
                s = Test.TestIt(resource);
                _loadedSprites.Add(resource, new WeakReference<Sprite>(s));
            }
            else if (!w.TryGetTarget(out s))
            {
                s = Test.TestIt(resource);
                w.SetTarget(s);
            }
            return s;
        }

        private void UpdateCurrentFrame(TimeSpan timePassed)
        {
            int curFrameIndex = _curFrameIndex;
            int curFrameDelay = _sprite.Frames[curFrameIndex].Delay;
            if (curFrameDelay == -1)
            {
                return;
            }
            TimeSpan timeRequired = _nextFrameTime.Subtract(timePassed);
            long ms;
            for (ms = (long)timeRequired.TotalMilliseconds; ms <= 0; ms += (long)(curFrameDelay * SpeedModifier))
            {
                if (++curFrameIndex >= _sprite.Frames.Length)
                {
                    curFrameIndex = 0;
                }
                curFrameDelay = _sprite.Frames[curFrameIndex].Delay;
                if (curFrameDelay == -1)
                {
                    _curFrameIndex = curFrameIndex;
                    return;
                }
            }
            _curFrameIndex = curFrameIndex;
            _nextFrameTime = TimeSpan.FromMilliseconds(ms);
        }
        public static void UpdateCurrentFrameForAll(TimeSpan timePassed)
        {
            foreach (WeakReference<AnimatedSprite> w in _loadedAnimSprites)
            {
                if (!w.TryGetTarget(out AnimatedSprite s))
                {
                    continue;
                }
                s.UpdateCurrentFrame(timePassed);
            }
        }
    }
}
