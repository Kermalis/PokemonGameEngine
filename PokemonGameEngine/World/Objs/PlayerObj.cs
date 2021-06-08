using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Script;
using System;

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

        #region Interaction

        public bool CanUseSurfFromCurrentPosition()
        {
            if (State == PlayerObjState.Surfing)
            {
                return false; // Cannot use surf if we're surfing
            }
            Position p = Pos;
            if (!IsInteractionLegal(Facing, Map, p.X, p.Y, out int x, out int y, out Map map))
            {
                return false; // Can only use surf if we can reach an interaction
            }
            if (!Overworld.IsSurfable(map.GetBlock_InBounds(x, y).BlocksetBlock.Behavior))
            {
                return false; // Can only surf on water
            }
            if (!IsMovementLegal(Facing, true))
            {
                return false; // Cannot surf if we're blocked diagonally
            }
            return true;
        }
        private bool CanLoadInteractionScript(string script, bool isSurfing)
        {
            if (script is null)
            {
                return false;
            }
            if (script == Overworld.SurfScript)
            {
                if (isSurfing)
                {
                    return false; // Disallow the surf script if we're surfing already
                }
                if (!IsMovementLegal(Facing, true))
                {
                    return false; // Disallow surf interaction if we're blocked diagonally
                }
            }
            return true;
        }

        private bool CheckForAInteraction()
        {
            if (!InputManager.IsPressed(Key.A))
            {
                return false;
            }

            // TODO: This does not consider sideways stairs or countertops when fetching the target block
            // TODO: Stuff like signs
            Position p = Pos;
            if (!IsInteractionLegal(Facing, Map, p.X, p.Y, out int x, out int y, out Map map))
            {
                return false;
            }

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
            if (CanLoadInteractionScript(scr, State == PlayerObjState.Surfing))
            {
                ScriptLoader.LoadScript(scr);
                return true;
            }

            return false;
        }

        private static bool IsInteractionLegal(FacingDirection facing, Map map, int x, int y, out int outX, out int outY, out Map outMap)
        {
            switch (facing)
            {
                case FacingDirection.South:
                {
                    return CanInteract_Cardinal(map, x, y,
                        x, y + 1, BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_N,
                        out outX, out outY, out outMap);
                }
                case FacingDirection.North:
                {
                    return CanInteract_Cardinal(map, x, y,
                        x, y - 1, BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_S,
                        out outX, out outY, out outMap);
                }
                case FacingDirection.West:
                {
                    return CanInteract_Cardinal(map, x, y,
                        x - 1, y, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_E,
                        out outX, out outY, out outMap);
                }
                case FacingDirection.East:
                {
                    return CanInteract_Cardinal(map, x, y,
                        x + 1, y, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_W,
                        out outX, out outY, out outMap);
                }
                case FacingDirection.Southwest:
                {
                    return CanInteract_Diagonal(map, x, y,
                        x - 1, y + 1, LayoutBlockPassage.SoutheastPassage, x - 1, y, LayoutBlockPassage.NorthwestPassage, x, y + 1,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_SE, BlocksetBlockBehavior.Blocked_NW,
                        out outX, out outY, out outMap);
                }
                case FacingDirection.Southeast:
                {
                    return CanInteract_Diagonal(map, x, y,
                        x + 1, y + 1, LayoutBlockPassage.SouthwestPassage, x + 1, y, LayoutBlockPassage.NortheastPassage, x, y + 1,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_SW, BlocksetBlockBehavior.Blocked_NE,
                        out outX, out outY, out outMap);
                }
                case FacingDirection.Northwest:
                {
                    return CanInteract_Diagonal(map, x, y,
                        x - 1, y - 1, LayoutBlockPassage.NortheastPassage, x - 1, y, LayoutBlockPassage.SouthwestPassage, x, y - 1,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_NE, BlocksetBlockBehavior.Blocked_SW,
                        out outX, out outY, out outMap);
                }
                case FacingDirection.Northeast:
                {
                    return CanInteract_Diagonal(map, x, y,
                        x + 1, y - 1, LayoutBlockPassage.NorthwestPassage, x + 1, y, LayoutBlockPassage.SoutheastPassage, x, y - 1,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_NW, BlocksetBlockBehavior.Blocked_SE,
                        out outX, out outY, out outMap);
                }
                default: throw new ArgumentOutOfRangeException(nameof(facing));
            }
        }

        // South/North/West/East
        private static bool CanInteract_Cardinal(Map curMap, int curX, int curY,
            int targetX, int targetY, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget,
            out int outX, out int outY, out Map outMap)
        {
            // Current block - return false if we are blocked
            Map.Layout.Block curBlock = curMap.GetBlock_InBounds(curX, curY);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrent)
            {
                outX = default;
                outY = default;
                outMap = default;
                return false;
            }
            // Target block - return false if we are blocked
            curMap.GetXYMap(targetX, targetY, out outX, out outY, out outMap);
            Map.Layout.Block targetBlock = outMap.GetBlock_InBounds(outX, outY);
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if (targetBehavior == blockedTarget)
            {
                return false;
            }
            return true;
        }
        // Southwest/Southeast/Northwest/Northeast
        private static bool CanInteract_Diagonal(Map curMap, int curX, int curY,
            int targetX, int targetY, LayoutBlockPassage neighbor1Passage, int neighbor1X, int neighbor1Y, LayoutBlockPassage neighbor2Passage, int neighbor2X, int neighbor2Y,
            BlocksetBlockBehavior blockedCurrentCardinal1, BlocksetBlockBehavior blockedCurrentCardinal2, BlocksetBlockBehavior blockedCurrentDiagonal,
            BlocksetBlockBehavior blockedTargetCardinal1, BlocksetBlockBehavior blockedTargetCardinal2, BlocksetBlockBehavior blockedTargetDiagonal,
            BlocksetBlockBehavior blockedNeighbor1, BlocksetBlockBehavior blockedNeighbor2,
            out int outX, out int outY, out Map outMap)
        {
            // Current block - return false if we are blocked
            Map.Layout.Block curBlock = curMap.GetBlock_InBounds(curX, curY);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrentCardinal1 || curBehavior == blockedCurrentCardinal2 || curBehavior == blockedCurrentDiagonal)
            {
                outX = default;
                outY = default;
                outMap = default;
                return false;
            }
            // Target block - return false if we are blocked
            curMap.GetXYMap(targetX, targetY, out outX, out outY, out outMap);
            Map.Layout.Block targetBlock = outMap.GetBlock_InBounds(outX, outY);
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if (targetBehavior == blockedTargetCardinal1 || targetBehavior == blockedTargetCardinal2 || targetBehavior == blockedTargetDiagonal)
            {
                return false;
            }
            // Target's neighbors - check if we can interact through them diagonally
            if (!CanInteractThroughDiagonally(curMap, neighbor1X, neighbor1Y, neighbor1Passage, blockedCurrentCardinal1, blockedTargetCardinal2, blockedNeighbor1)
                || !CanInteractThroughDiagonally(curMap, neighbor2X, neighbor2Y, neighbor2Passage, blockedTargetCardinal1, blockedCurrentCardinal2, blockedNeighbor2))
            {
                return false;
            }
            return true;
        }
        private static bool CanInteractThroughDiagonally(Map map, int x, int y, LayoutBlockPassage diagonalPassage,
            BlocksetBlockBehavior blockedCardinal1, BlocksetBlockBehavior blockedCardinal2, BlocksetBlockBehavior blockedDiagonal)
        {
            // Get the x/y/map of the block
            map.GetXYMap(x, y, out int outX, out int outY, out Map outMap);
            Map.Layout.Block block = outMap.GetBlock_InBounds(outX, outY);
            // Check occupancy permission
            if ((block.Passage & diagonalPassage) == 0)
            {
                return false;
            }
            // Check block behaviors
            BlocksetBlockBehavior blockBehavior = block.BlocksetBlock.Behavior;
            if (blockBehavior == blockedCardinal1 || blockBehavior == blockedCardinal2 || blockBehavior == blockedDiagonal)
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}
