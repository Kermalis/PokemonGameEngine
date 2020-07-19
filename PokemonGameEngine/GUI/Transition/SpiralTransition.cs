using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class SpiralTransition
    {
        // This will leave artifacts if NumBoxes is not cleanly divisible by the width and height of the screen
        // In the future we can have it draw bigger squares if it's not cleanly divisible (like, outside the bounds of the screen)
        private const int NumBoxes = 8;

        private int _counterX;
        private int _counterY;
        private int _target;
        private int _numCompleted;
        private bool doX;
        private bool goBackwards;
        private Action _onTransitionEnded;

        public SpiralTransition(Action onTransitionEnded)
        {
            _onTransitionEnded = onTransitionEnded;
            doX = true;
            _target = NumBoxes - 1;
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            // Draw current spiral
            int boxWidth = bmpWidth / NumBoxes;
            int boxHeight = bmpHeight / NumBoxes;
            RenderUtils.FillColor(bmpAddress, bmpWidth, bmpHeight, _counterX * boxWidth, _counterY * boxHeight, boxWidth, boxHeight, 0xFF000000);

            // If it is done (we want to draw the final box before we say this is done)
            if (doX
                ? goBackwards ? _counterX < _target : _counterX > _target
                : goBackwards ? _counterY < _target : _counterY > _target)
            {
                _onTransitionEnded.Invoke();
                _onTransitionEnded = null;
                return;
            }

            // Update the spiral
            if (doX)
            {
                if (goBackwards)
                {
                    if (_counterX == _target)
                    {
                        doX = false;
                        _counterY--;
                        _target++;
                    }
                    else
                    {
                        _counterX--;
                    }
                }
                else
                {
                    if (_counterX == _target)
                    {
                        doX = false;
                        _counterY++;
                    }
                    else
                    {
                        _counterX++;
                    }
                }
            }
            else
            {
                if (goBackwards)
                {
                    if (_counterY == _target)
                    {
                        doX = true;
                        goBackwards = false;
                        _counterX++;
                        _numCompleted++;
                        _target = NumBoxes - 1 - _numCompleted;
                    }
                    else
                    {
                        _counterY--;
                    }
                }
                else
                {
                    if (_counterY == _target)
                    {
                        doX = true;
                        goBackwards = true;
                        _counterX--;
                        _target = _numCompleted;
                    }
                    else
                    {
                        _counterY++;
                    }
                }
            }
        }
    }
}
