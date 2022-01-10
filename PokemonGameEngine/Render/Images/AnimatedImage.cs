using Kermalis.PokemonGameEngine.Render.GUIs;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal sealed partial class AnimatedImage : IImage
    {
        private static readonly List<AnimatedImage> _loadedAnimImages = new();

        private readonly Instance _img;
        /// <summary>Will be paused after <see cref="_repeatCount"/> is achieved. Unpausing after will do nothing but cause it to pause again</summary>
        private readonly int _repeatCount;
        private int _numRepeats;

        private int _curFrameIndex;
        private float _curFrameTimeRemaining;
        public float SpeedModifier;
        public bool IsPaused;

        public uint Texture => _img.Frames[_curFrameIndex].Texture;
        public Vec2I Size => _img.Size;

        public AnimatedImage(string asset, (uint PID, bool Shiny)? spindaSpots = null, bool isPaused = false, float speedModifier = 1, int? repeatCount = null)
        {
            _img = Instance.LoadOrGet(asset, spindaSpots);
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

        public void Render(Vec2I pos, bool xFlip = false, bool yFlip = false)
        {
            GUIRenderer.Texture(Texture, Rect.FromSize(pos, Size), new UV(xFlip, yFlip));
        }

        public void Restart()
        {
            float frameLen = _img.Frames[0].SecondsVisible;
            if (frameLen == Instance.Frame.STAY_FOREVER)
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
            if (curFrameLen == Instance.Frame.STAY_FOREVER)
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
                    if (_repeatCount != Instance.REPEAT_FOREVER && ++_numRepeats >= _repeatCount)
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
                if (curFrameLen == Instance.Frame.STAY_FOREVER)
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

        public void DeductReference()
        {
            _loadedAnimImages.Remove(this);
            _img.DeductReference();
        }
    }
}
