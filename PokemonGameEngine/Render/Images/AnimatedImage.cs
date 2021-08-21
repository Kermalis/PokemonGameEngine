using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.SimpleGIF;
using Kermalis.SimpleGIF.Decoding;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal sealed class AnimatedImage : IImage
    {
        private sealed class Frame
        {
            public uint Texture { get; }
            public int Delay { get; }

            public unsafe Frame(GL gl, DecodedGIF.Frame frame, int w, int h)
            {
                Texture = GLHelper.GenTexture(gl);
                GLHelper.BindTexture(gl, Texture);
                fixed (void* imgdata = frame.Bitmap)
                {
                    GLTextureUtils.LoadTextureData(gl, imgdata, (uint)w, (uint)h);
                }
                Delay = frame.Delay;
            }
        }
        private sealed class Image
        {
            public Frame[] Frames { get; }
            public Size2D Size { get; }
            public ushort RepeatCount { get; } // 0 means forever

            private Image(string resource, string id, (uint PID, bool Shiny)? spindaSpots)
            {
                GL gl = Game.OpenGL;
                DecodedGIF gif = GIFRenderer.DecodeAllFrames(Utils.GetResourceStream(resource), ColorFormat.RGBA);
                if (spindaSpots.HasValue)
                {
                    (uint pid, bool shiny) = spindaSpots.Value;
                    SpindaSpotRenderer.Render(gif, pid, shiny);
                }
                GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
                Frames = new Frame[gif.Frames.Count];
                for (int i = 0; i < gif.Frames.Count; i++)
                {
                    Frames[i] = new Frame(gl, gif.Frames[i], gif.Width, gif.Height);
                }
                Size = new Size2D((uint)gif.Width, (uint)gif.Height);
                RepeatCount = gif.RepeatCount;
                _id = id;
                _numReferences = 1;
                _loadedImages.Add(id, this);
            }

            #region Cache

            private readonly string _id;
            private int _numReferences;
            private static readonly Dictionary<string, Image> _loadedImages = new();
            public static Image LoadOrGet(string resource, (uint PID, bool Shiny)? spindaSpots)
            {
                // Add spinda spot data to the resource to use it uniquely
                string id;
                if (spindaSpots.HasValue)
                {
                    (uint pid, bool shiny) = spindaSpots.Value;
                    id = resource + string.Format("_{0:X8}_{1}", pid, shiny);
                }
                else
                {
                    id = resource;
                }
                // LoadOrGet now
                if (_loadedImages.TryGetValue(id, out Image img))
                {
                    img._numReferences++;
                }
                else
                {
                    img = new Image(resource, id, spindaSpots);
                }
                return img;
            }

            public void DeductReference(GL gl)
            {
                if (--_numReferences <= 0)
                {
                    for (int i = 0; i < Frames.Length; i++)
                    {
                        gl.DeleteTexture(Frames[i].Texture);
                    }
                    _loadedImages.Remove(_id);
                }
            }

            #endregion
        }

        private readonly Image _img;
        private readonly int _repeatCount; // Will be paused after _repeatCount is achieved, and unpausing will do nothing but cause it to pause again (in that situation)
        public float SpeedModifier;
        public bool IsPaused;

        public uint Texture => _img.Frames[_curFrameIndex].Texture;
        public Size2D Size => _img.Size;
        public int NumFrames => _img.Frames.Length;
        private int _numRepeats;
        private int _curFrameIndex;
        private TimeSpan _nextFrameTime;

        public AnimatedImage(string resource, (uint PID, bool Shiny)? spindaSpots = null, bool isPaused = false, float speedModifier = 1, int? repeatCount = null)
        {
            _img = Image.LoadOrGet(resource, spindaSpots);
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
            _loadedAnimImages.Add(this);
        }

        public void Render(Pos2D pos, bool xFlip = false, bool yFlip = false)
        {
            GUIRenderer.Instance.RenderTexture(Texture, new Rect2D(pos, Size), xFlip: xFlip, yFlip: yFlip);
        }
        public void RenderFrame(int i, Pos2D pos, bool xFlip = false, bool yFlip = false)
        {
            GUIRenderer.Instance.RenderTexture(_img.Frames[i].Texture, new Rect2D(pos, Size), xFlip: xFlip, yFlip: yFlip);
        }

        private void UpdateCurrentFrame()
        {
            int curFrameIndex = _curFrameIndex;
            int curFrameDelay = _img.Frames[curFrameIndex].Delay;
            if (curFrameDelay == -1)
            {
                IsPaused = true;
                return; // This would only be reached if IsPaused is set to false manually
            }
            TimeSpan timeRequired = _nextFrameTime.Subtract(Game.RenderTimeSinceLastFrame);
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
            foreach (AnimatedImage i in _loadedAnimImages)
            {
                if (!i.IsPaused)
                {
                    i.UpdateCurrentFrame();
                }
            }
        }

        private static readonly List<AnimatedImage> _loadedAnimImages = new();

        public void DeductReference(GL gl)
        {
            _loadedAnimImages.Remove(this);
            _img.DeductReference(gl);
        }
        public static void DeleteAll(GL gl)
        {
            foreach (AnimatedImage i in _loadedAnimImages)
            {
                i.DeductReference(gl);
            }
        }
    }
}
