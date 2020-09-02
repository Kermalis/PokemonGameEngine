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
            if (!CanMoveWillingly)
            {
                return;
            }
            // Check the current block after moving for a trigger or for the behavior
            if (_shouldRunTriggers)
            {
                _shouldRunTriggers = false; // #12 - Do not return before setting FinishedMoving to false
                Position playerPos = Pos;
                // ScriptTile
                foreach (Map.Events.ScriptEvent se in Map.MapEvents.ScriptTiles)
                {
                    if (playerPos.IsSamePosition(se) && se.VarConditional.Match(Game.Instance.Save.Vars[se.Var], se.VarValue))
                    {
                        string script = se.Script;
                        if (script != string.Empty)
                        {
                            ScriptLoader.LoadScript(script);
                            return;
                        }
                    }
                }
                // Warp
                foreach (Map.Events.WarpEvent warp in Map.MapEvents.Warps)
                {
                    if (playerPos.IsSamePosition(warp))
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

            if (InputManager.IsPressed(Key.Start))
            {
                Game.Instance.OpenStartMenu();
                return;
            }
            if (InputManager.IsPressed(Key.A))
            {
                // TODO: This does not consider sideways stairs or countertops when fetching the target block
                // TODO: Stuff like surf and signs
                // TODO: Block behaviors that start scripts (like bookshelves, tvs, and the PC)
                Position p = Pos;
                int x = p.X;
                int y = p.Y;
                switch (Facing)
                {
                    case FacingDirection.South: y++; break;
                    case FacingDirection.Southwest: x--; y++; break;
                    case FacingDirection.Southeast: x++; y++; break;
                    case FacingDirection.North: y--; break;
                    case FacingDirection.Northwest: x--; y--; break;
                    case FacingDirection.Northeast: x++; y--; break;
                    case FacingDirection.West: x--; break;
                    case FacingDirection.East: x++; break;
                }
                Map.GetXYMap(x, y, out x, out y, out Map map);
                //BlocksetBlockBehavior beh = map.GetBlock_InBounds(x, y).BlocksetBlock.Behavior;
                foreach (EventObj o in map.GetObjs_InBounds(x, y, p.Elevation, this, false))
                {
                    string script = o.Script;
                    if (script != string.Empty)
                    {
                        Game.Instance.Save.Vars[Var.LastTalked] = (short)o.Id; // Special var for the last person we talked to
                        ScriptLoader.LoadScript(script); // Load script after LastTalked is set
                        return;
                    }
                }
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
