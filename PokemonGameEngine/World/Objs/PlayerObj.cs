﻿using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Script;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class PlayerObj : VisualObj
    {
        public static readonly PlayerObj Player = new();

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
            Player.Pos.X = x;
            Player.Pos.Y = y;
            Player.Map = map;
            map.Objs.Add(Player);
        }

        protected override void OnMapChanged(Map oldMap, Map newMap)
        {
            Overworld.DoEnteredMapThings(newMap);
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
                    OverworldGUI.Instance.SetInteractiveScript(o, script);
                    return true;
                }
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
            bool run = InputManager.IsDown(Key.B);
            Position oldP = Pos;
            Move(facing, run, false);
            if (!oldP.IsSamePosition(Pos))
            {
                Game.Instance.Save.GameStats[GameStat.StepsTaken]++;
                _changedPosition = true;
            }
            _shouldRunTriggers = true;
        }
    }
}
