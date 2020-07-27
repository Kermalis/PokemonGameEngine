using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Overworld;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Script;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class OverworldGUI
    {
        private bool _shouldRunTriggers;

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
            // Check the current block after moving for a trigger or for the behavior
            if (_shouldRunTriggers)
            {
                _shouldRunTriggers = false; // #12 - Do not return before setting FinishedMoving to false
                if (Overworld.Overworld.CheckForWildBattle(false))
                {
                    return;
                }
            }

            foreach (ScriptContext ctx in Game.Game.Instance.Scripts.ToArray()) // Copy the list so a script ending/starting does not crash here
            {
                ctx.LogicTick();
            }

            if (Game.Game.Instance.Scripts.Count == 0 && InputManager.IsPressed(Key.A)) // Temporary
            {
                ScriptLoader.LoadScript("TestScript");
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
            _shouldRunTriggers = true;
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.FillColor(bmpAddress, bmpWidth, bmpHeight, 0xFF000000);
            Map.Draw(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
