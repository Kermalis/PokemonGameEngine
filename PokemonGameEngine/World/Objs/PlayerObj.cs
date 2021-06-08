using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Script;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal enum PlayerObjState : byte
    {
        Walking,
        Biking,
        Surfing
    }

    internal sealed class PlayerObj : VisualObj
    {
        public static readonly PlayerObj Player = new();

        public PlayerObjState State;

        public bool IsWaitingForObjToStartScript;
        public override bool CanMoveWillingly => !IsWaitingForObjToStartScript && base.CanMoveWillingly;

        private bool _shouldRunTriggers;
        private bool _changedPosition;

        private PlayerObj()
            : base(Overworld.PlayerId, "Player")
        {
        }
        public static void Init(int x, int y, Map map)
        {
            Player.State = PlayerObjState.Walking;
            Player.Pos.X = x;
            Player.Pos.Y = y;
            Player.Map = map;
            map.Objs.Add(Player);
        }

        protected override void OnMapChanged(Map oldMap, Map newMap)
        {
            Overworld.DoEnteredMapThings(newMap);
        }
        protected override void OnDismountFromWater()
        {
            State = PlayerObjState.Walking;
        }
        protected override bool CanSurf()
        {
            return State == PlayerObjState.Surfing;
        }
        protected override bool IsSurfing()
        {
            return State == PlayerObjState.Surfing;
        }

        public bool CanUseSurfFromCurrentPosition()
        {
            // Check if we can move to the target block (consider diagonally or a fence behavior)
            return Overworld.IsSurfable(GetBlockFacing().BlocksetBlock.Behavior) && IsMovementLegal(Facing, true);
        }

        private bool CheckForThingsAfterMovement()
        {
            if (!_shouldRunTriggers)
            {
                return false;
            }

            _shouldRunTriggers = false; // #12 - Do not return without setting to false, otherwise this will be checked many times in a row
            bool moved = _changedPosition;
            _changedPosition = false;

            if (moved)
            {
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
                            return true;
                        }
                    }
                }

                // Warp
                foreach (Map.Events.WarpEvent warp in Map.MapEvents.Warps)
                {
                    if (playerPos.IsSamePosition(warp))
                    {
                        OverworldGUI.Instance.TempWarp(warp);
                        return true;
                    }
                }
            }

            // Battle
            if (Encounter.CheckForWildBattle(false))
            {
                return true;
            }

            if (moved)
            {
                // Friendship
                Friendship.UpdateFriendshipStep();
                // Daycare
                Game.Instance.Save.Daycare.DoDaycareStep();
                // Egg
                Game.Instance.Save.Daycare.DoEggCycleStep();
                // Hatch
                Party pp = Game.Instance.Save.PlayerParty;
                for (short i = 0; i < pp.Count; i++)
                {
                    PartyPokemon p = pp[i];
                    if (p.IsEgg && p.Friendship == 0)
                    {
                        Game.Instance.Save.Vars[Var.SpecialVar1] = i;
                        ScriptLoader.LoadScript("Egg_Hatch");
                        return true;
                    }
                }
            }

            return false;
        }
        private bool CheckForAInteraction()
        {
            if (!InputManager.IsPressed(Key.A))
            {
                return false;
            }

            // TODO: This does not consider sideways stairs or countertops when fetching the target block
            // TODO: Stuff like signs
            // TODO: Can you move to the surf block legally?
            // TODO: Don't allow surf if there's someone in the way (IsMovementLegal)
            Position p = Pos;
            Overworld.MoveCoords(Facing, p.X, p.Y, out int x, out int y);
            Map.GetXYMap(x, y, out x, out y, out Map map);
            // Talk to someone on our elevation
            foreach (EventObj o in map.GetObjs_InBounds(x, y, p.Elevation, this, false))
            {
                string script = o.Script;
                if (script != string.Empty)
                {
                    OverworldGUI.Instance.SetInteractiveScript(o, script);
                    return true;
                }
            }
            // Talk to block (like Surf)
            BlocksetBlockBehavior beh = map.GetBlock_InBounds(x, y).BlocksetBlock.Behavior;
            string scr = Overworld.GetBlockBehaviorScript(beh);
            // Disallow the surf script if we're surfing
            if (scr is not null && !(scr == Overworld.SurfScript && State == PlayerObjState.Surfing))
            {
                ScriptLoader.LoadScript(scr);
                return true;
            }
            return false;
        }
        public override void LogicTick()
        {
            if (!CanMoveWillingly)
            {
                return;
            }
            if (CheckForThingsAfterMovement())
            {
                return;
            }

            if (InputManager.IsPressed(Key.Start))
            {
                OverworldGUI.Instance.OpenStartMenu();
                return;
            }
            if (CheckForAInteraction())
            {
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
            bool run = State == PlayerObjState.Walking && InputManager.IsDown(Key.B);
            if (Move(facing, run, false))
            {
                Game.Instance.Save.GameStats[GameStat.StepsTaken]++;
                _changedPosition = true;
            }
            _shouldRunTriggers = true;
        }

        protected override int GetImage(bool showMoving)
        {
            byte f = (byte)Facing;
            if (State is PlayerObjState.Surfing or PlayerObjState.Biking)
            {
                return f + 24;
            }
            if (!showMoving)
            {
                return f;
            }
            return _leg ? f + 8 : f + 16;
        }
    }
}
