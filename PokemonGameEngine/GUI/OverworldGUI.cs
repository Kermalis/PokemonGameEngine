using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Overworld;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Script;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class OverworldGUI
    {
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
                if (Overworld.Overworld.CheckForWildBattle(false))
                {
                    return;
                }
            }

            if (InputManager.IsPressed(Key.A)) // Temporary
            {
                ScriptContext ctx = ScriptLoader.LoadScript("TestScript");
                while (!ctx.TempDone)
                {
                    ctx.RunNextCommand();
                }
                return;
            }

            bool down = InputManager.IsDown(Key.Down);
            bool up = InputManager.IsDown(Key.Up);
            bool left = InputManager.IsDown(Key.Left);
            bool right = InputManager.IsDown(Key.Right);
            if (!down && !up && !left && !right)
            {
                return;
            }
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
            Obj.Player.Move(facing, run, false);
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.FillColor(bmpAddress, bmpWidth, bmpHeight, 0xFF000000);
            Map.Draw(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
