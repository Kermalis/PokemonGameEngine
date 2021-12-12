using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class SpiralTransition : FadeColorTransition
    {
        // This will leave artifacts if NUM_RECTS_PER_AXIS is not cleanly divisible by the width and height of the screen
        // In the future we can have it draw bigger rects if it's not cleanly divisible (like, outside the bounds of the screen)
        private const int NUM_RECTS_PER_AXIS = 8;

        private readonly float _secondsPerRect;
        private float _time;
        private int _counter;

        public SpiralTransition(float secondsPerRect = 0.02f)
        {
            _secondsPerRect = secondsPerRect;
        }

        public override void Render()
        {
            if (!IsDone)
            {
                _time += Display.DeltaTime;
                _counter = (int)(_time / _secondsPerRect);
            }
            SpiralTransitionLogic(GLHelper.CurrentSize);
        }

        private void SpiralTransitionLogic(Size2D dstSize)
        {
            int counterX = 0;
            int counterY = 0;
            int target = NUM_RECTS_PER_AXIS - 1;
            int numCompleted = 0;
            bool doX = true;
            bool goBackwards = false;

            uint boxWidth = dstSize.Width / NUM_RECTS_PER_AXIS;
            uint boxHeight = dstSize.Height / NUM_RECTS_PER_AXIS;
            for (int i = 0; i <= _counter; i++)
            {
                // Draw
                GUIRenderer.Instance.FillRectangle(Colors.Black4, new Rect2D(new Pos2D(counterX * (int)boxWidth, counterY * (int)boxHeight), new Size2D(boxWidth, boxHeight)));

                // If it is done (we want to draw the final box before we say this is done)
                if (doX
                    ? goBackwards ? counterX < target : counterX > target
                    : goBackwards ? counterY < target : counterY > target)
                {
                    IsDone = true;
                    return;
                }

                // Update the spiral
                if (doX)
                {
                    if (goBackwards)
                    {
                        if (counterX == target)
                        {
                            doX = false;
                            counterY--;
                            target++;
                        }
                        else
                        {
                            counterX--;
                        }
                    }
                    else
                    {
                        if (counterX == target)
                        {
                            doX = false;
                            counterY++;
                        }
                        else
                        {
                            counterX++;
                        }
                    }
                }
                else
                {
                    if (goBackwards)
                    {
                        if (counterY == target)
                        {
                            doX = true;
                            goBackwards = false;
                            counterX++;
                            numCompleted++;
                            target = NUM_RECTS_PER_AXIS - 1 - numCompleted;
                        }
                        else
                        {
                            counterY--;
                        }
                    }
                    else
                    {
                        if (counterY == target)
                        {
                            doX = true;
                            goBackwards = true;
                            counterX--;
                            target = numCompleted;
                        }
                        else
                        {
                            counterY++;
                        }
                    }
                }
            }
        }
    }
}
