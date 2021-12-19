﻿using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.SimpleGIF;
using Kermalis.SimpleGIF.Decoding;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal sealed class AnimatedImage : IImage
    {
        private sealed class Frame
        {
            public const float STAY_FOREVER = -1f;

            public readonly float SecondsVisible;
            public readonly uint Texture;

            public unsafe Frame(GL gl, DecodedGIF.Frame frame, Size2D size)
            {
                int? d = frame.Delay;
                SecondsVisible = d is null ? STAY_FOREVER : d.Value / 1_000f;

                Texture = gl.GenTexture();
                gl.BindTexture(TextureTarget.Texture2D, Texture);
                fixed (uint* imgdata = frame.Bitmap)
                {
#if DEBUG_ANIMIMG_HITBOX
                    Debug_AddHitbox(imgdata, size);
#endif
                    GLTextureUtils.LoadTextureData(gl, imgdata, size);
                }
            }

#if DEBUG_ANIMIMG_HITBOX
            private unsafe void Debug_AddHitbox(uint* imgdata, Size2D size)
            {
                Pos2D pos;
                for (pos.Y = 0; pos.Y < size.Height; pos.Y++)
                {
                    for (pos.X = 0; pos.X < size.Width; pos.X++)
                    {
                        uint* p = Renderer.GetPixelAddress(imgdata, size.Width, pos);
                        if (*p != 0)
                        {
                            continue;
                        }
                        if ((pos.X == 0 && pos.Y == 0)
                            || (pos.X == 0 && pos.Y == size.Height - 1)
                            || (pos.X == size.Width - 1 && pos.Y == 0)
                            || (pos.X == size.Width - 1 && pos.Y == size.Height - 1))
                        {
                            *p = Renderer.RawColor(0, 0, 255, 255); // Corners
                        }
                        else if (pos.X == 0 || pos.Y == 0
                            || pos.X == size.Width - 1 || pos.Y == size.Height - 1)
                        {
                            *p = Renderer.RawColor(255, 0, 0, 255); // Borders
                        }
                        else // Inside
                        {
                            uint a = (uint)(pos.X % 4 / 3f * 255);
                            uint b = (uint)(pos.Y % 4 / 3f * 255);
                            bool horizontal = true;
                            bool vertical = true;
                            if (horizontal && vertical)
                            {
                                *p = Renderer.RawColor(a, b, a, 255);
                            }
                            else if (horizontal)
                            {
                                *p = Renderer.RawColor(0, b, 0, 255);
                            }
                            else if (vertical)
                            {
                                *p = Renderer.RawColor(a, 0, a, 255);
                            }
                        }
                    }
                }
            }
#endif
        }
        private sealed class Image
        {
            public const ushort REPEAT_FOREVER = 0;

            public Frame[] Frames { get; }
            public Size2D Size { get; }
            public ushort RepeatCount { get; }

            private Image(string asset, string id, (uint PID, bool Shiny)? spindaSpots)
            {
                DecodedGIF gif = GIFRenderer.DecodeAllFrames(AssetLoader.GetAssetStream(asset), ColorFormat.RGBA);

                if (spindaSpots is not null)
                {
                    (uint pid, bool shiny) = spindaSpots.Value;
                    SpindaSpotRenderer.Render(gif, pid, shiny);
                }

                GL gl = Display.OpenGL;
                gl.ActiveTexture(TextureUnit.Texture0);

                Size = new Size2D((uint)gif.Width, (uint)gif.Height);
                Frames = new Frame[gif.Frames.Count];
                for (int i = 0; i < gif.Frames.Count; i++)
                {
                    Frames[i] = new Frame(gl, gif.Frames[i], Size);
                }
                RepeatCount = gif.RepeatCount;
                _id = id;
                _numReferences = 1;
                _loadedImages.Add(id, this);
            }

            #region Cache

            private readonly string _id;
            private int _numReferences;
            private static readonly Dictionary<string, Image> _loadedImages = new();
            public static Image LoadOrGet(string asset, (uint PID, bool Shiny)? spindaSpots)
            {
                // Add spinda spot data to the asset to use it uniquely
                string id;
                if (spindaSpots is not null)
                {
                    (uint pid, bool shiny) = spindaSpots.Value;
                    id = asset + string.Format("_{0:X8}_{1}", pid, shiny);
                }
                else
                {
                    id = asset;
                }
                // LoadOrGet now
                if (_loadedImages.TryGetValue(id, out Image img))
                {
                    img._numReferences++;
                }
                else
                {
                    img = new Image(asset, id, spindaSpots);
                }
                return img;
            }

            public void DeductReference()
            {
                if (--_numReferences <= 0)
                {
                    GL gl = Display.OpenGL;
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
        private float _curFrameTimeRemaining;

        public AnimatedImage(string asset, (uint PID, bool Shiny)? spindaSpots = null, bool isPaused = false, float speedModifier = 1, int? repeatCount = null)
        {
            _img = Image.LoadOrGet(asset, spindaSpots);
            if (isPaused)
            {
                IsPaused = true;
            }
            else
            {
                Restart();
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

        public void Restart()
        {
            float frameLen = _img.Frames[0].SecondsVisible;
            if (frameLen == Frame.STAY_FOREVER)
            {
                IsPaused = true;
            }
            else
            {
                _curFrameTimeRemaining = frameLen;
                IsPaused = false;
            }
        }

        private void Update_Internal()
        {
            int curFrameIndex = _curFrameIndex;
            float curFrameLen = _img.Frames[curFrameIndex].SecondsVisible;
            if (curFrameLen == Frame.STAY_FOREVER)
            {
                IsPaused = true;
                return; // This would only be reached if IsPaused is set to false manually
            }

            // Advance time remaining by the speed modifier
            // If the time is <= 0, it's time for a new frame (multiple could've been skipped)
            _curFrameTimeRemaining -= Display.DeltaTime * SpeedModifier;
            while (_curFrameTimeRemaining <= 0f)
            {
                if (curFrameIndex + 1 >= _img.Frames.Length)
                {
                    // Currently on the last frame, pause or loop?
                    if (_repeatCount != Image.REPEAT_FOREVER && ++_numRepeats >= _repeatCount)
                    {
                        IsPaused = true;
                        _curFrameIndex = curFrameIndex;
                        return; // No more repeats allowed, pause it and return
                    }
                    curFrameIndex = 0; // Loop to first frame
                }
                else
                {
                    curFrameIndex++; // Go to next frame
                }

                // We are starting a new frame
                _curFrameIndex = curFrameIndex;
                curFrameLen = _img.Frames[curFrameIndex].SecondsVisible;
                if (curFrameLen == Frame.STAY_FOREVER)
                {
                    IsPaused = true;
                    return; // The new frame is set to stay forever, pause and return
                }
                // Set new frame time remaining
                _curFrameTimeRemaining += curFrameLen;
            }
        }
        public void Update()
        {
            if (!IsPaused)
            {
                Update_Internal();
            }
        }
        public static void UpdateAll()
        {
            foreach (AnimatedImage i in _loadedAnimImages)
            {
                if (!i.IsPaused)
                {
                    i.Update_Internal();
                }
            }
        }

        private static readonly List<AnimatedImage> _loadedAnimImages = new();

        public void DeductReference()
        {
            _loadedAnimImages.Remove(this);
            _img.DeductReference();
        }
    }
}
