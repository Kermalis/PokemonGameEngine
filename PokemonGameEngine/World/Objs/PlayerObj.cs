using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Script;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class PlayerObj : VisualObj
    {
        public static readonly PlayerObj Player = new PlayerObj();

        private bool _shouldRunTriggers;

        private PlayerObj()
            : base(Overworld.PlayerId, "Player")
        {
        }

        public override void LogicTick()
        {
            if (!CanMove)
            {
                return;
            }
            // Check the current block after moving for a trigger or for the behavior
            if (_shouldRunTriggers)
            {
                _shouldRunTriggers = false; // #12 - Do not return before setting FinishedMoving to false
                Position playerPos = Pos;
                // Warp
                foreach (Map.Events.WarpEvent warp in Map.MapEvents.Warps)
                {
                    if (playerPos.X == warp.X && playerPos.Y == warp.Y && playerPos.Elevation == warp.Elevation)
                    {
                        Game.Instance.TempWarp(warp);
                        return;
                    }
                }
                // Battle
                if (Overworld.CheckForWildBattle(false))
                {
                    return;
                }
            }

            foreach (ScriptContext ctx in Game.Instance.Scripts.ToArray()) // Copy the list so a script ending/starting does not crash here
            {
                ctx.LogicTick();
            }

            if (Game.Instance.Scripts.Count == 0 && InputManager.IsPressed(Key.A)) // Temporary
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
            FacingDirection facing;
            if (down)
            {
                if (left)
                {
                    facing = FacingDirection.Southwest;
                }
                else if (right)
                {
                    facing = FacingDirection.Southeast;
                }
                else
                {
                    facing = FacingDirection.South;
                }
            }
            else if (up)
            {
                if (left)
                {
                    facing = FacingDirection.Northwest;
                }
                else if (right)
                {
                    facing = FacingDirection.Northeast;
                }
                else
                {
                    facing = FacingDirection.North;
                }
            }
            else if (left)
            {
                facing = FacingDirection.West;
            }
            else
            {
                facing = FacingDirection.East;
            }
            bool run = InputManager.IsDown(Key.B);
            Move(facing, run, false);
            _shouldRunTriggers = true;
        }
    }
}
