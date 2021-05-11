using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.Util;
using Kermalis.SimpleGIF;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class AnimatedImage : IImage
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
        private sealed class Image
        {
            public Frame[] Frames { get; }
            public int Width { get; }
            public int Height { get; }
            public ushort RepeatCount { get; } // 0 means forever

            private Image(string resource)
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

            private static readonly Dictionary<string, WeakReference<Image>> _loadedImages = new Dictionary<string, WeakReference<Image>>();
            public static Image LoadOrGet(string resource, bool useCache)
            {
                Image i;
                if (useCache)
                {
                    if (!_loadedImages.TryGetValue(resource, out WeakReference<Image> w))
                    {
                        i = new Image(resource);
                        _loadedImages.Add(resource, new WeakReference<Image>(i));
                    }
                    else if (!w.TryGetTarget(out i))
                    {
                        i = new Image(resource);
                        w.SetTarget(i);
                    }
                }
                else
                {
                    i = new Image(resource);
                }
                return i;
            }
        }

        private readonly Image _img;
        private readonly int _repeatCount; // Will be paused after _repeatCount is achieved, and unpausing will do nothing but cause it to pause again (in that situation)
        public double SpeedModifier;
        public bool IsPaused;

        public uint[] Bitmap => _img.Frames[_curFrameIndex].Bitmap;
        public int Width => _img.Width;
        public int Height => _img.Height;
        public int NumFrames => _img.Frames.Length;
        private int _numRepeats;
        private int _curFrameIndex;
        private TimeSpan _nextFrameTime;

        public AnimatedImage(string resource, bool useCache, bool isPaused = false, double speedModifier = 1, int? repeatCount = null)
        {
            _img = Image.LoadOrGet(resource, useCache);
            int frameDelay = _img.Frames[0].Delay;
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
            _repeatCount = repeatCount ?? _img.RepeatCount;

            for (int i = 0; i < _loadedAnimImages.Count; i++)
            {
                WeakReference<AnimatedImage> w = _loadedAnimImages[i];
                if (!w.TryGetTarget(out _))
                {
                    w.SetTarget(this);
                    return;
                }
            }
            _loadedAnimImages.Add(new WeakReference<AnimatedImage>(this));
        }

        private static readonly List<WeakReference<AnimatedImage>> _loadedAnimImages = new List<WeakReference<AnimatedImage>>();

        private void UpdateCurrentFrame()
        {
            int curFrameIndex = _curFrameIndex;
            int curFrameDelay = _img.Frames[curFrameIndex].Delay;
            if (curFrameDelay == -1)
            {
                IsPaused = true;
                return; // This would only be reached if IsPaused is set to false manually
            }
            TimeSpan timeRequired = _nextFrameTime.Subtract(Program.RenderTimeSinceLastFrame);
            long ms;
            for (ms = (long)timeRequired.TotalMilliseconds; ms <= 0; ms += (long)(curFrameDelay * SpeedModifier))
            {
                if (curFrameIndex + 1 >= _img.Frames.Length)
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
                curFrameDelay = _img.Frames[curFrameIndex].Delay;
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
        public static void UpdateCurrentFrameForAll()
        {
            foreach (WeakReference<AnimatedImage> w in _loadedAnimImages)
            {
                if (!w.TryGetTarget(out AnimatedImage s))
                {
                    continue;
                }
                if (!s.IsPaused)
                {
                    s.UpdateCurrentFrame();
                }
            }
        }

        public uint[] GetFrameBitmap(int frame)
        {
            return _img.Frames[frame].Bitmap;
        }
    }
}
