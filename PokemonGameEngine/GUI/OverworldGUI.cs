using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Overworld;
using Kermalis.PokemonGameEngine.Render;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class OverworldGUI
    {
        private const int TempWildBattleRate = 10;

        private bool CheckForWildBattle()
        {
            bool ret = Map.GetBlock(Obj.Player).BlocksetBlock.Behavior == BlocksetBlockBehavior.WildBattle && PBEUtils.GlobalRandom.RandomBool(TempWildBattleRate, 100);
            if (ret)
            {
                Game.Game.TempCreateBattle();
            }
            return ret;
        }

        public void LogicTick()
        {
            List<Obj> list = Obj.LoadedObjs;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                list[i].UpdateMovementTimer();
            }
            if (!Obj.Camera.CanMove || !Obj.Player.CanMove)
            {
                return;
            }
            bool check = Obj.Player.FinishedMoving;
            for (int i = 0; i < count; i++)
            {
                list[i].FinishedMoving = false;
            }
            if (check) // #12 - Do not return before setting FinishedMoving to false
            {
                // Check the current tile after moving for a trigger or for the behavior
                if (CheckForWildBattle())
                {
                    return;
                }
            }

            bool down = InputManager.IsDown(Key.Down);
            bool up = InputManager.IsDown(Key.Up);
            bool left = InputManager.IsDown(Key.Left);
            bool right = InputManager.IsDown(Key.Right);
            if (down || up || left || right)
            {
                Obj.FacingDirection facing;
                if (down)
                {
                    if (left)
                    {
                        facing = Obj.FacingDirection.Southwest;
                    }
                    else if (right)
                    {
                        facing = Obj.FacingDirection.Southeast;
                    }
                    else
                    {
                        facing = Obj.FacingDirection.South;
                    }
                }
                else if (up)
                {
                    if (left)
                    {
                        facing = Obj.FacingDirection.Northwest;
                    }
                    else if (right)
                    {
                        facing = Obj.FacingDirection.Northeast;
                    }
                    else
                    {
                        facing = Obj.FacingDirection.North;
                    }
                }
                else if (left)
                {
                    facing = Obj.FacingDirection.West;
                }
                else
                {
                    facing = Obj.FacingDirection.East;
                }
                bool run = InputManager.IsDown(Key.B);
                // TODO: Lock camera at a specific xy offset away from the player
                if (Obj.Player.Move(facing, run))
                {
                    Obj.Camera.CopyXY(Obj.Player);
                }
            }
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.FillColor(bmpAddress, bmpWidth, bmpHeight, 0xFF000000);
            Map.Draw(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
