using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.World;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.World.Maps;
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
        public static readonly PlayerObj Instance = new();

        public PlayerObjState State;

        public bool IsWaitingForObjToStartScript;
        public override bool CanMoveWillingly => !IsWaitingForObjToStartScript && base.CanMoveWillingly;

        private bool _shouldRunTriggers;
        private bool _changedPosition;

        private PlayerObj()
            : base(Overworld.PlayerId, "Player")
        {
        }
        public static void Init(in WorldPos pos, Map map)
        {
            Instance.State = PlayerObjState.Walking;
            Instance.Pos = pos;
            Instance.Map = map;
            map.Objs.Add(Instance);
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
                WorldPos playerPos = Pos;

                // ScriptTile
                foreach (MapEvents.ScriptEvent se in Map.Events.ScriptTiles)
                {
                    if (playerPos.Equals(se.Pos) && se.VarConditional.Match(Game.Instance.Save.Vars[se.Var], se.VarValue))
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
                foreach (MapEvents.WarpEvent warp in Map.Events.Warps)
                {
                    if (playerPos.Equals(warp.Pos))
                    {
                        OverworldGUI.Instance.TempWarp(warp.Warp);
                        return true;
                    }
                }
            }

            // Battle
            if (EncounterMaker.CheckForWildBattle(false))
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
        public override void Update()
        {
            if (!CanMoveWillingly)
            {
                return;
            }
            if (CheckForThingsAfterMovement())
            {
                return;
            }

            if (InputManager.JustPressed(Key.Start))
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

        protected override uint GetImage(bool showMoving)
        {
            byte f = (byte)Facing;
            if (State is PlayerObjState.Surfing or PlayerObjState.Biking)
            {
                return f + 24u;
            }
            if (!showMoving)
            {
                return f;
            }
            return _leg ? f + 8u : f + 16u;
        }

        #region Interaction

        public bool CanUseSurfFromCurrentPosition()
        {
            if (State == PlayerObjState.Surfing)
            {
                return false; // Cannot use surf if we're surfing
            }
            WorldPos p = Pos;
            if (!IsInteractionLegal(Facing, p.XY, Map, out Pos2D talkXY, out Map talkMap))
            {
                return false; // Can only use surf if we can reach an interaction
            }
            if (!Overworld.IsSurfable(talkMap.GetBlock_InBounds(talkXY).BlocksetBlock.Behavior))
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
            if (!InputManager.JustPressed(Key.A))
            {
                return false;
            }

            // TODO: This does not consider sideways stairs or countertops when fetching the target block
            // TODO: Stuff like signs
            WorldPos p = Pos;
            if (!IsInteractionLegal(Facing, p.XY, Map, out Pos2D talkXY, out Map talkMap))
            {
                return false;
            }

            // Talk to someone on our elevation
            foreach (EventObj o in talkMap.GetObjs_InBounds(new WorldPos(talkXY, p.Elevation), this, false))
            {
                string script = o.Script;
                if (script != string.Empty)
                {
                    OverworldGUI.Instance.SetInteractiveScript(o, script);
                    return true;
                }
            }

            // Talk to block (like Surf)
            BlocksetBlockBehavior beh = talkMap.GetBlock_InBounds(talkXY).BlocksetBlock.Behavior;
            string scr = Overworld.GetBlockBehaviorScript(beh);
            if (CanLoadInteractionScript(scr, State == PlayerObjState.Surfing))
            {
                ScriptLoader.LoadScript(scr);
                return true;
            }

            return false;
        }

        private static bool IsInteractionLegal(FacingDirection facing, Pos2D curXY, Map curMap, out Pos2D targetXY, out Map targetMap)
        {
            switch (facing)
            {
                case FacingDirection.South:
                {
                    return CanInteract_Cardinal(curMap, curXY,
                        curXY.South(), BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_N,
                        out targetXY, out targetMap);
                }
                case FacingDirection.North:
                {
                    return CanInteract_Cardinal(curMap, curXY,
                        curXY.North(), BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_S,
                        out targetXY, out targetMap);
                }
                case FacingDirection.West:
                {
                    return CanInteract_Cardinal(curMap, curXY,
                        curXY.West(), BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_E,
                        out targetXY, out targetMap);
                }
                case FacingDirection.East:
                {
                    return CanInteract_Cardinal(curMap, curXY,
                        curXY.East(), BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_W,
                        out targetXY, out targetMap);
                }
                case FacingDirection.Southwest:
                {
                    return CanInteract_Diagonal(curMap, curXY,
                        curXY.Southwest(), LayoutBlockPassage.SoutheastPassage, curXY.West(), LayoutBlockPassage.NorthwestPassage, curXY.South(),
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_SE, BlocksetBlockBehavior.Blocked_NW,
                        out targetXY, out targetMap);
                }
                case FacingDirection.Southeast:
                {
                    return CanInteract_Diagonal(curMap, curXY,
                        curXY.Southeast(), LayoutBlockPassage.SouthwestPassage, curXY.East(), LayoutBlockPassage.NortheastPassage, curXY.South(),
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_SW, BlocksetBlockBehavior.Blocked_NE,
                        out targetXY, out targetMap);
                }
                case FacingDirection.Northwest:
                {
                    return CanInteract_Diagonal(curMap, curXY,
                        curXY.Northwest(), LayoutBlockPassage.NortheastPassage, curXY.West(), LayoutBlockPassage.SouthwestPassage, curXY.North(),
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_NE, BlocksetBlockBehavior.Blocked_SW,
                        out targetXY, out targetMap);
                }
                case FacingDirection.Northeast:
                {
                    return CanInteract_Diagonal(curMap, curXY,
                        curXY.Northeast(), LayoutBlockPassage.NorthwestPassage, curXY.East(), LayoutBlockPassage.SoutheastPassage, curXY.North(),
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_NW, BlocksetBlockBehavior.Blocked_SE,
                        out targetXY, out targetMap);
                }
                default: throw new ArgumentOutOfRangeException(nameof(facing));
            }
        }

        // South/North/West/East
        private static bool CanInteract_Cardinal(Map curMap, Pos2D curXY,
            Pos2D targetXY, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget,
            out Pos2D newTargetXY, out Map targetMap)
        {
            // Current block - return false if we are blocked
            MapLayout.Block curBlock = curMap.GetBlock_InBounds(curXY);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrent)
            {
                newTargetXY = default;
                targetMap = default;
                return false;
            }
            // Target block - return false if we are blocked
            curMap.GetXYMap(targetXY, out newTargetXY, out targetMap);
            MapLayout.Block targetBlock = targetMap.GetBlock_InBounds(newTargetXY);
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if (targetBehavior == blockedTarget)
            {
                return false;
            }
            return true;
        }
        // Southwest/Southeast/Northwest/Northeast
        private static bool CanInteract_Diagonal(Map curMap, Pos2D curXY,
            Pos2D targetXY, LayoutBlockPassage neighbor1Passage, Pos2D neighbor1XY, LayoutBlockPassage neighbor2Passage, Pos2D neighbor2XY,
            BlocksetBlockBehavior blockedCurrentCardinal1, BlocksetBlockBehavior blockedCurrentCardinal2, BlocksetBlockBehavior blockedCurrentDiagonal,
            BlocksetBlockBehavior blockedTargetCardinal1, BlocksetBlockBehavior blockedTargetCardinal2, BlocksetBlockBehavior blockedTargetDiagonal,
            BlocksetBlockBehavior blockedNeighbor1, BlocksetBlockBehavior blockedNeighbor2,
            out Pos2D newTargetXY, out Map targetMap)
        {
            // Current block - return false if we are blocked
            MapLayout.Block curBlock = curMap.GetBlock_InBounds(curXY);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrentCardinal1 || curBehavior == blockedCurrentCardinal2 || curBehavior == blockedCurrentDiagonal)
            {
                newTargetXY = default;
                targetMap = default;
                return false;
            }
            // Target block - return false if we are blocked
            curMap.GetXYMap(targetXY, out newTargetXY, out targetMap);
            MapLayout.Block targetBlock = targetMap.GetBlock_InBounds(newTargetXY);
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if (targetBehavior == blockedTargetCardinal1 || targetBehavior == blockedTargetCardinal2 || targetBehavior == blockedTargetDiagonal)
            {
                return false;
            }
            // Target's neighbors - check if we can interact through them diagonally
            if (!CanInteractThroughDiagonally(curMap, neighbor1XY, neighbor1Passage, blockedCurrentCardinal1, blockedTargetCardinal2, blockedNeighbor1)
                || !CanInteractThroughDiagonally(curMap, neighbor2XY, neighbor2Passage, blockedTargetCardinal1, blockedCurrentCardinal2, blockedNeighbor2))
            {
                return false;
            }
            return true;
        }
        private static bool CanInteractThroughDiagonally(Map map, Pos2D xy, LayoutBlockPassage diagonalPassage,
            BlocksetBlockBehavior blockedCardinal1, BlocksetBlockBehavior blockedCardinal2, BlocksetBlockBehavior blockedDiagonal)
        {
            // Get the x/y/map of the block
            map.GetXYMap(xy, out xy, out map);
            MapLayout.Block block = map.GetBlock_InBounds(xy);
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

        public override void Dispose()
        {
            throw new InvalidOperationException();
        }
    }
}
