using Kermalis.PokemonGameEngine.Render;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class SpiralTransition : FadeColorTransition
    {
        // This will leave artifacts if NumBoxes is not cleanly divisible by the width and height of the screen
        // In the future we can have it draw bigger squares if it's not cleanly divisible (like, outside the bounds of the screen)
        private const int NumBoxes = 8;

        private int _counter;

        private unsafe void SpiralTransitionLogic(uint* bmpAddress, int bmpWidth, int bmpHeight, int num)
        {
            int counterX = 0;
            int counterY = 0;
            int target = NumBoxes - 1;
            int numCompleted = 0;
            bool doX = true;
            bool goBackwards = false;

            int boxWidth = bmpWidth / NumBoxes;
            int boxHeight = bmpHeight / NumBoxes;
            for (int i = 0; i <= num; i++)
            {
                // Draw
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, counterX * boxWidth, counterY * boxHeight, boxWidth, boxHeight, RenderUtils.Color(0, 0, 0, 255));

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

        public unsafe override void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            SpiralTransitionLogic(bmpAddress, bmpWidth, bmpHeight, _counter);
            if (!IsDone)
            {
                _counter++;
            }
        }
    }
}
