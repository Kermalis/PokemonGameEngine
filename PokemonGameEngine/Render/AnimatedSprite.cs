using Kermalis.PokemonGameEngine.Util;
using Kermalis.SimpleGIF;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class AnimatedSprite : ISprite
    {
        private sealed class Frame
        {
            public uint[] Bitmap { get; }
            public int Delay { get; }

            public Frame(DecodedGIF.Frame frame)
            {
                Bitmap = (uint[])frame.Bitmap.Clone();
                Delay = frame.Delay;
            }
        }
        private sealed class Sprite
        {
            public Frame[] Frames { get; }
            public int Width { get; }
            public int Height { get; }
            public ushort RepeatCount { get; } // 0 means forever

            private Sprite(string resource)
            {
                DecodedGIF gif = GIFRenderer.DecodeAllFrames(Utils.GetResourceStream(resource), SimpleGIF.Decoding.ColorFormat.RGBA);
                Frames = new Frame[gif.Frames.Count];
                for (int i = 0; i < gif.Frames.Count; i++)
                {
                    Frames[i] = new Frame(gif.Frames[i]);
                }
                Width = gif.Width;
                Height = gif.Height;
                RepeatCount = gif.RepeatCount;
            }

            private static readonly Dictionary<string, WeakReference<Sprite>> _loadedSprites = new Dictionary<string, WeakReference<Sprite>>();
            public static Sprite LoadOrGet(string resource, bool useCache)
            {
                Sprite s;
                if (useCache)
                {
                    if (!_loadedSprites.TryGetValue(resource, out WeakReference<Sprite> w))
                    {
                        s = new Sprite(resource);
                        _loadedSprites.Add(resource, new WeakReference<Sprite>(s));
                    }
                    else if (!w.TryGetTarget(out s))
                    {
                        s = new Sprite(resource);
                        w.SetTarget(s);
                    }
                }
                else
                {
                    s = new Sprite(resource);
                }
                return s;
            }
        }

        private readonly Sprite _sprite;
        private readonly int _repeatCount; // Will be paused after _repeatCount is achieved, and unpausing will do nothing but cause it to pause again (in that situation)
        public double SpeedModifier;
        public bool IsPaused;

        public uint[] Bitmap => _sprite.Frames[_curFrameIndex].Bitmap;
        public int Width => _sprite.Width;
        public int Height => _sprite.Height;
        public int NumFrames => _sprite.Frames.Length;
        private int _numRepeats;
        private int _curFrameIndex;
        private TimeSpan _nextFrameTime;

        public AnimatedSprite(string resource, bool useCache, bool isPaused = false, double speedModifier = 1, int? repeatCount = null)
        {
            _sprite = Sprite.LoadOrGet(resource, useCache);
            int frameDelay = _sprite.Frames[0].Delay;
            if (frameDelay == -1)
            {
                IsPaused = true;
            }
            else
            {
                _nextFrameTime = TimeSpan.FromMilliseconds(frameDelay);
                IsPaused = isPaused;
            }
            SpeedModifier = speedModifier;
            _repeatCount = repeatCount ?? _sprite.RepeatCount;

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

        private void UpdateCurrentFrame(TimeSpan timePassed)
        {
            int curFrameIndex = _curFrameIndex;
            int curFrameDelay = _sprite.Frames[curFrameIndex].Delay;
            if (curFrameDelay == -1)
            {
                IsPaused = true;
                return; // This would only be reached if IsPaused is set to false manually
            }
            TimeSpan timeRequired = _nextFrameTime.Subtract(timePassed);
            long ms;
            for (ms = (long)timeRequired.TotalMilliseconds; ms <= 0; ms += (long)(curFrameDelay * SpeedModifier))
            {
                if (curFrameIndex + 1 >= _sprite.Frames.Length)
                {
                    if (_repeatCount != 0 && ++_numRepeats >= _repeatCount)
                    {
                        IsPaused = true;
                        _curFrameIndex = curFrameIndex;
                        return;
                    }
                    curFrameIndex = 0;
                }
                else
                {
                    curFrameIndex++;
                }
                curFrameDelay = _sprite.Frames[curFrameIndex].Delay;
                if (curFrameDelay == -1)
                {
                    IsPaused = true;
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
                if (!s.IsPaused)
                {
                    s.UpdateCurrentFrame(timePassed);
                }
            }
        }

        public uint[] GetFrameBitmap(int frame)
        {
            return _sprite.Frames[frame].Bitmap;
        }
    }
}
