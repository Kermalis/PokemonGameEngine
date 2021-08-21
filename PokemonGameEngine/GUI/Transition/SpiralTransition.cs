using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class SpiralTransition : FadeColorTransition
    {
        // This will leave artifacts if NumBoxes is not cleanly divisible by the width and height of the screen
        // In the future we can have it draw bigger squares if it's not cleanly divisible (like, outside the bounds of the screen)
        private const int NumBoxes = 8;
        private const int MillisecondsPerBox = 20;

        private TimeSpan _cur;
        private int _counter;

        public SpiralTransition()
        {
            _cur = new TimeSpan();
        }

        private void SpiralTransitionLogic(uint dstW, uint dstH, int num)
        {
            int counterX = 0;
            int counterY = 0;
            int target = NumBoxes - 1;
            int numCompleted = 0;
            bool doX = true;
            bool goBackwards = false;

            uint boxWidth = dstW / NumBoxes;
            uint boxHeight = dstH / NumBoxes;
            for (int i = 0; i <= num; i++)
            {
                // Draw
                GUIRenderer.Instance.FillRectangle(Colors.Black, new Rect2D(new Pos2D(counterX * (int)boxWidth, counterY * (int)boxHeight), new Size2D(boxWidth, boxHeight)));

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
                            target = NumBoxes - 1 - numCompleted;
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

        public override void Render(GL gl)
        {
            if (!IsDone)
            {
                _cur += Game.RenderTimeSinceLastFrame;
                _counter = (int)(_cur.TotalMilliseconds / MillisecondsPerBox);
            }
            SpiralTransitionLogic(GLHelper.CurrentWidth, GLHelper.CurrentHeight, _counter);
            if (!IsDone)
            {
                _counter++;
            }
        }
    }
}
