﻿using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.World;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.Scripts;
using Kermalis.PokemonGameEngine.Sound;
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
        public static PlayerObj Instance { get; private set; } = null!; // Set in Init()

        public PlayerObjState State;

        public bool IsWaitingForObjToStartScript;
        public override bool CanMoveWillingly => !IsWaitingForObjToStartScript && base.CanMoveWillingly;

        private bool _shouldRunTriggers;
        private bool _changedPosition;

        private PlayerObj()
            : base(Overworld.PlayerId, "Player")
        {
        }
        public static void Init(in WorldPos pos, Map map, PlayerObjState state)
        {
            Instance = new PlayerObj();
            Instance.State = state;
            Instance.Pos = pos;
            Instance.Map = map;
        }

        protected override void OnMapChanged(Map oldMap, Map newMap)
        {
            base.OnMapChanged(oldMap, newMap);
            Overworld.OnPlayerMapChanged();
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

        public void Warp()
        {
            WarpInProgress wip = WarpInProgress.Current;
            WarpInProgress.Current = null;
            MusicPlayer.Main.FadeToQueuedMusic(); // Start music now. The callback when the CameraObj's map changes will not change the music after this
            Map oldMap = Map;
            Map newMap = wip.DestMap;
            SetMap(newMap); // Move player to the new map first. If the camera were moved first, the old map would unload with the player on it
            Overworld.OnCameraMapChanged(oldMap, newMap);
            newMap.OnNoLongerWarpingMap();

            WorldPos newPos = wip.Destination.DestPos;
            MapLayout.Block block = newMap.GetBlock_InBounds(newPos.XY);

            // Facing is of the original direction unless the block behavior says otherwise
            // All QueuedScriptMovements will be run after the warp is complete
            switch (block.BlocksetBlock.Behavior)
            {
                case BlocksetBlockBehavior.Warp_WalkSouthOnExit:
                {
                    Facing = FacingDirection.South;
                    QueuedScriptMovements.Enqueue(ScriptMovement.Walk_S);
                    break;
                }
                case BlocksetBlockBehavior.Warp_NoOccupancy_S:
                {
                    Facing = FacingDirection.North;
                    newPos.XY.Y--;
                    break;
                }
            }

            Pos = newPos;
            MovingFromPos = newPos;
            VisualOfs = new Vec2I(0, 0);
            MovingFromVisualOfs = VisualOfs;
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
                    if (playerPos == se.Pos && se.VarConditional.Match(Game.Instance.Save.Vars[se.Var], se.VarValue))
                    {
                        string script = se.Script;
                        if (script != string.Empty)
                        {
                            OverworldGUI.Instance.StartScript(script);
                            return true;
                        }
                    }
                }

                // Warp
                foreach (MapEvents.WarpEvent warp in Map.Events.Warps)
                {
                    if (playerPos == warp.Pos)
                    {
                        OverworldGUI.Instance.StartPlayerWarp(warp.Warp);
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
                        OverworldGUI.Instance.StartScript("Egg_Hatch");
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
            WorldPos p = Pos;
            if (!IsInteractionLegal(Facing, p.XY, Map, out Vec2I talkXY, out Map talkMap))
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
            if (script == Overworld.SCRIPT_SURF)
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
            if (!IsInteractionLegal(Facing, p.XY, Map, out Vec2I talkXY, out Map talkMap))
            {
                return false;
            }

            // Talk to someone on our elevation
            Obj o = talkMap.GetNonCamObj_InBounds(new WorldPos(talkXY, p.Elevation), false);
            if (o is EventObj eo)
            {
                string script = eo.Script;
                if (script != string.Empty)
                {
                    OverworldGUI.Instance.SetInteractiveScript(eo, script);
                    return true;
                }
            }

            // Talk to block (like Surf)
            BlocksetBlockBehavior beh = talkMap.GetBlock_InBounds(talkXY).BlocksetBlock.Behavior;
            string scr = Overworld.GetBlockBehaviorScript(beh);
            if (CanLoadInteractionScript(scr, State == PlayerObjState.Surfing))
            {
                OverworldGUI.Instance.StartScript(scr);
                return true;
            }

            return false;
        }

        private static bool IsInteractionLegal(FacingDirection facing, Vec2I curXY, Map curMap, out Vec2I targetXY, out Map targetMap)
        {
            switch (facing)
            {
                case FacingDirection.South:
                {
                    return CanInteract_Cardinal(curMap, curXY,
                        curXY.Plus(0, 1), BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_N,
                        out targetXY, out targetMap);
                }
                case FacingDirection.North:
                {
                    return CanInteract_Cardinal(curMap, curXY,
                        curXY.Plus(0, -1), BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_S,
                        out targetXY, out targetMap);
                }
                case FacingDirection.West:
                {
                    return CanInteract_Cardinal(curMap, curXY,
                        curXY.Plus(-1, 0), BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_E,
                        out targetXY, out targetMap);
                }
                case FacingDirection.East:
                {
                    return CanInteract_Cardinal(curMap, curXY,
                        curXY.Plus(1, 0), BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_W,
                        out targetXY, out targetMap);
                }
                case FacingDirection.Southwest:
                {
                    return CanInteract_Diagonal(curMap, curXY,
                        curXY.Plus(-1, 1), LayoutBlockPassage.SoutheastPassage, curXY.Plus(-1, 0), LayoutBlockPassage.NorthwestPassage, curXY.Plus(0, 1),
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_SE, BlocksetBlockBehavior.Blocked_NW,
                        out targetXY, out targetMap);
                }
                case FacingDirection.Southeast:
                {
                    return CanInteract_Diagonal(curMap, curXY,
                        curXY.Plus(1, 1), LayoutBlockPassage.SouthwestPassage, curXY.Plus(1, 0), LayoutBlockPassage.NortheastPassage, curXY.Plus(0, 1),
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_SW, BlocksetBlockBehavior.Blocked_NE,
                        out targetXY, out targetMap);
                }
                case FacingDirection.Northwest:
                {
                    return CanInteract_Diagonal(curMap, curXY,
                        curXY.Plus(-1, -1), LayoutBlockPassage.NortheastPassage, curXY.Plus(-1, 0), LayoutBlockPassage.SouthwestPassage, curXY.Plus(0, -1),
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_NE, BlocksetBlockBehavior.Blocked_SW,
                        out targetXY, out targetMap);
                }
                case FacingDirection.Northeast:
                {
                    return CanInteract_Diagonal(curMap, curXY,
                        curXY.Plus(1, -1), LayoutBlockPassage.NorthwestPassage, curXY.Plus(1, 0), LayoutBlockPassage.SoutheastPassage, curXY.Plus(0, -1),
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_NW, BlocksetBlockBehavior.Blocked_SE,
                        out targetXY, out targetMap);
                }
                default: throw new ArgumentOutOfRangeException(nameof(facing));
            }
        }

        // South/North/West/East
        private static bool CanInteract_Cardinal(Map curMap, Vec2I curXY,
            Vec2I targetXY, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget,
            out Vec2I newTargetXY, out Map targetMap)
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
            curMap.GetPosAndMap(targetXY, out newTargetXY, out targetMap);
            MapLayout.Block targetBlock = targetMap.GetBlock_InBounds(newTargetXY);
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if (targetBehavior == blockedTarget)
            {
                return false;
            }
            return true;
        }
        // Southwest/Southeast/Northwest/Northeast
        private static bool CanInteract_Diagonal(Map curMap, Vec2I curXY,
            Vec2I targetXY, LayoutBlockPassage neighbor1Passage, Vec2I neighbor1XY, LayoutBlockPassage neighbor2Passage, Vec2I neighbor2XY,
            BlocksetBlockBehavior blockedCurrentCardinal1, BlocksetBlockBehavior blockedCurrentCardinal2, BlocksetBlockBehavior blockedCurrentDiagonal,
            BlocksetBlockBehavior blockedTargetCardinal1, BlocksetBlockBehavior blockedTargetCardinal2, BlocksetBlockBehavior blockedTargetDiagonal,
            BlocksetBlockBehavior blockedNeighbor1, BlocksetBlockBehavior blockedNeighbor2,
            out Vec2I newTargetXY, out Map targetMap)
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
            curMap.GetPosAndMap(targetXY, out newTargetXY, out targetMap);
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
        private static bool CanInteractThroughDiagonally(Map map, Vec2I xy, LayoutBlockPassage diagonalPassage,
            BlocksetBlockBehavior blockedCardinal1, BlocksetBlockBehavior blockedCardinal2, BlocksetBlockBehavior blockedDiagonal)
        {
            // Get the x/y/map of the block
            map.GetPosAndMap(xy, out xy, out map);
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
