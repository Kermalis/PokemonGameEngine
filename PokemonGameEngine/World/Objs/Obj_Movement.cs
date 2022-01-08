using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World.Maps;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract partial class Obj
    {
        // Move speed constants represent 1/x seconds to complete the movement
        private const float TURNING_MOVE_SPEED = 5f;
        private const float WALK_MOVE_SPEED = 4f;
        private const float RUN_MOVE_SPEED = 6f;
        private const float DIAGONAL_MOVE_SPEED_MOD = 0.7071067811865475f; // (2 / (sqrt((2^2) + (2^2)))
        private const float BLOCKED_MOVE_SPEED_MOD = 0.8f;
        private const int STAIR_Y_OFFSET = +6; // Any offset will work

        private static bool ElevationCheck(BlocksetBlockBehavior curBehavior, BlocksetBlockBehavior targetBehavior, byte curElevation, byte targetElevations)
        {
            if (targetElevations.HasElevation(curElevation))
            {
                return true;
            }
            return Overworld.AllowsElevationChange(curBehavior) || Overworld.AllowsElevationChange(targetBehavior);
        }
        private bool CanMoveTo_Cardinal__CanOccupy(Vec2I targetXY, bool allowSurf,
            BlocksetBlockBehavior curBehavior, BlocksetBlockBehavior blockedTarget, bool checkElevation, byte curElevation)
        {
            // Get the x/y/map of the target block
            Map.GetXYMap(targetXY, out targetXY, out Map targetMap);
            MapLayout.Block targetBlock = targetMap.GetBlock_InBounds(targetXY);
            // Check occupancy permission
            if ((targetBlock.Passage & LayoutBlockPassage.AllowOccupancy) == 0)
            {
                return false;
            }
            // Check block behaviors
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if ((!allowSurf && Overworld.IsSurfable(targetBehavior)) || targetBehavior == blockedTarget)
            {
                return false;
            }
            // Check elevation
            byte targetElevations = targetBlock.Elevations;
            if (checkElevation && !ElevationCheck(curBehavior, targetBehavior, curElevation, targetElevations))
            {
                return false;
            }
            // Check if we are blocked by an obj at the position
            byte newElevation = Overworld.GetElevationIfMovedTo(curElevation, targetElevations);
            if (targetMap.GetNonCamObj_InBounds(new WorldPos(targetXY, newElevation), true) is not null)
            {
                return false;
            }
            return true;
        }
        // South/North
        private bool CanMoveTo_Cardinal(Vec2I targetXY, bool allowSurf, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget)
        {
            // Current block - return false if we are blocked
            WorldPos p = Pos;
            MapLayout.Block curBlock = Map.GetBlock_InBounds(p.XY);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrent)
            {
                return false;
            }
            // Target block - return false if we are blocked
            if (!CanMoveTo_Cardinal__CanOccupy(targetXY, allowSurf, curBehavior, blockedTarget, true, p.Elevation))
            {
                return false;
            }
            return true;
        }
        // West/East
        private bool CanMoveTo_Cardinal_ConsiderStairs(Vec2I targetXY, bool allowSurf, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget,
            BlocksetBlockBehavior upBehavior, BlocksetBlockBehavior downBehavior)
        {
            // Current block - return false if we are blocked
            WorldPos p = Pos;
            MapLayout.Block curBlock = Map.GetBlock_InBounds(p.XY);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrent)
            {
                return false;
            }
            // Stairs - check if we can go up a stair that's above our target
            Map.GetXYMap(targetXY.Plus(0, -1), out Vec2I upStairPos, out Map upStairMap);
            MapLayout.Block upStairBlock = upStairMap.GetBlock_InBounds(upStairPos);
            if ((upStairBlock.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
            {
                BlocksetBlockBehavior upStairBehavior = upStairBlock.BlocksetBlock.Behavior;
                if (upStairBehavior == upBehavior)
                {
                    // Check if we are blocked by an obj at the position
                    byte newElevation = Overworld.GetElevationIfMovedTo(p.Elevation, upStairBlock.Elevations);
                    if (upStairMap.GetNonCamObj_InBounds(new WorldPos(upStairPos, newElevation), true) is not null)
                    {
                        return false;
                    }
                    return true;
                }
            }
            bool canChangeElevation = false;
            // Stairs - If we are on a down stair, then we will be going to the block diagonally below us
            if (curBehavior == downBehavior)
            {
                canChangeElevation = true;
                targetXY.Y++;
            }
            else if (curBehavior == upBehavior)
            {
                canChangeElevation = true;
            }
            // Target block - return false if we are blocked
            if (!CanMoveTo_Cardinal__CanOccupy(targetXY, allowSurf, curBehavior, blockedTarget, !canChangeElevation, p.Elevation))
            {
                return false;
            }
            return true;
        }
        // Southwest/Southeast/Northwest/Northeast
        private bool CanMoveTo_Diagonal(Vec2I targetXY, bool allowSurf, LayoutBlockPassage neighbor1Passage, Vec2I neighbor1XY, LayoutBlockPassage neighbor2Passage, Vec2I neighbor2XY,
            BlocksetBlockBehavior blockedCurrentCardinal1, BlocksetBlockBehavior blockedCurrentCardinal2, BlocksetBlockBehavior blockedCurrentDiagonal,
            BlocksetBlockBehavior blockedTargetCardinal1, BlocksetBlockBehavior blockedTargetCardinal2, BlocksetBlockBehavior blockedTargetDiagonal,
            BlocksetBlockBehavior blockedNeighbor1, BlocksetBlockBehavior blockedNeighbor2)
        {
            // Current block - return false if we are blocked
            WorldPos p = Pos;
            MapLayout.Block curBlock = Map.GetBlock_InBounds(p.XY);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrentCardinal1 || curBehavior == blockedCurrentCardinal2 || curBehavior == blockedCurrentDiagonal)
            {
                return false;
            }
            // Target block - return false if we are blocked
            Map.GetXYMap(targetXY, out targetXY, out Map targetMap);
            MapLayout.Block targetBlock = targetMap.GetBlock_InBounds(targetXY);
            if ((targetBlock.Passage & LayoutBlockPassage.AllowOccupancy) == 0)
            {
                return false;
            }
            // Check block behaviors
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if ((!allowSurf && Overworld.IsSurfable(targetBehavior))
                || targetBehavior == blockedTargetCardinal1 || targetBehavior == blockedTargetCardinal2 || targetBehavior == blockedTargetDiagonal)
            {
                return false;
            }
            // Check elevation
            byte curElevation = p.Elevation;
            byte targetElevations = targetBlock.Elevations;
            if (!ElevationCheck(curBehavior, targetBehavior, curElevation, targetElevations))
            {
                return false;
            }
            // Check if we are blocked by an obj at the position
            byte newElevation = Overworld.GetElevationIfMovedTo(curElevation, targetElevations);
            if (targetMap.GetNonCamObj_InBounds(new WorldPos(targetXY, newElevation), true) is not null)
            {
                return false;
            }
            // Target's neighbors - check if we can pass through them diagonally
            if (!CanPassThroughDiagonally(new WorldPos(neighbor1XY, curElevation), neighbor1Passage, blockedCurrentCardinal1, blockedTargetCardinal2, blockedNeighbor1)
                || !CanPassThroughDiagonally(new WorldPos(neighbor2XY, curElevation), neighbor2Passage, blockedTargetCardinal1, blockedCurrentCardinal2, blockedNeighbor2))
            {
                return false;
            }
            return true;
        }

        private bool CanPassThroughDiagonally(in WorldPos pos, LayoutBlockPassage diagonalPassage,
            BlocksetBlockBehavior blockedCardinal1, BlocksetBlockBehavior blockedCardinal2, BlocksetBlockBehavior blockedDiagonal)
        {
            // Get the x/y/map of the block
            Map.GetXYMap(pos.XY, out Vec2I targetXY, out Map targetMap);
            MapLayout.Block block = targetMap.GetBlock_InBounds(targetXY);
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
            // Check if we are blocked by an obj at the position (only checks current elevation, not the target elevation or any other elevations)
            if (targetMap.GetNonCamObj_InBounds(new WorldPos(targetXY, pos.Elevation), true) is not null)
            {
                return false;
            }
            return true;
        }

        public bool IsMovementLegal(FacingDirection facing, bool allowSurf)
        {
            Vec2I xy = Pos.XY;
            switch (facing)
            {
                case FacingDirection.South:
                {
                    return CanMoveTo_Cardinal(xy.Plus(0, 1), allowSurf, BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_N);
                }
                case FacingDirection.North:
                {
                    return CanMoveTo_Cardinal(xy.Plus(0, -1), allowSurf, BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_S);
                }
                case FacingDirection.West:
                {
                    return CanMoveTo_Cardinal_ConsiderStairs(xy.Plus(-1, 0), allowSurf, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_E,
                        BlocksetBlockBehavior.Stair_W, BlocksetBlockBehavior.Stair_E);
                }
                case FacingDirection.East:
                {
                    return CanMoveTo_Cardinal_ConsiderStairs(xy.Plus(1, 0), allowSurf, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_W,
                        BlocksetBlockBehavior.Stair_E, BlocksetBlockBehavior.Stair_W);
                }
                case FacingDirection.Southwest:
                {
                    return CanMoveTo_Diagonal(xy.Plus(-1, 1), allowSurf, LayoutBlockPassage.SoutheastPassage, xy.Plus(-1, 0), LayoutBlockPassage.NorthwestPassage, xy.Plus(0, 1),
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_SE, BlocksetBlockBehavior.Blocked_NW);
                }
                case FacingDirection.Southeast:
                {
                    return CanMoveTo_Diagonal(xy.Plus(1, 1), allowSurf, LayoutBlockPassage.SouthwestPassage, xy.Plus(1, 0), LayoutBlockPassage.NortheastPassage, xy.Plus(0, 1),
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_SW, BlocksetBlockBehavior.Blocked_NE);
                }
                case FacingDirection.Northwest:
                {
                    return CanMoveTo_Diagonal(xy.Plus(-1, -1), allowSurf, LayoutBlockPassage.NortheastPassage, xy.Plus(-1, 0), LayoutBlockPassage.SouthwestPassage, xy.Plus(0, -1),
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_NE, BlocksetBlockBehavior.Blocked_SW);
                }
                case FacingDirection.Northeast:
                {
                    return CanMoveTo_Diagonal(xy.Plus(1, -1), allowSurf, LayoutBlockPassage.NorthwestPassage, xy.Plus(1, 0), LayoutBlockPassage.SoutheastPassage, xy.Plus(0, -1),
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_NW, BlocksetBlockBehavior.Blocked_SE);
                }
                default: throw new ArgumentOutOfRangeException(nameof(facing));
            }
        }

        private void ApplyStairMovement(Vec2I curXY, BlocksetBlockBehavior upBehavior, BlocksetBlockBehavior downBehavior)
        {
            MapLayout.Block curBlock = Map.GetBlock_InBounds(curXY);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == downBehavior)
            {
                Pos.XY.Y++;
            }
            Vec2I newXY = Pos.XY;
            MapLayout.Block upStairBlock = Map.GetBlock_CrossMap(newXY.Plus(0, -1), out _, out _);
            BlocksetBlockBehavior upStairBehavior = upStairBlock.BlocksetBlock.Behavior;
            if (upStairBehavior == upBehavior)
            {
                Pos.XY.Y--;
                VisualOfs.Y = STAIR_Y_OFFSET;
                return;
            }
            MapLayout.Block newBlock = Map.GetBlock_CrossMap(newXY, out _, out _);
            BlocksetBlockBehavior newBehavior = newBlock.BlocksetBlock.Behavior;
            if (newBehavior == downBehavior)
            {
                VisualOfs.Y = STAIR_Y_OFFSET;
            }
            else
            {
                VisualOfs.Y = 0;
            }
        }
        private void ApplyMovement(FacingDirection facing)
        {
            switch (facing)
            {
                case FacingDirection.South:
                {
                    Pos.XY.Y++;
                    break;
                }
                case FacingDirection.North:
                {
                    Pos.XY.Y--;
                    break;
                }
                case FacingDirection.West:
                {
                    Vec2I xy = Pos.XY;
                    Pos.XY.X--;
                    ApplyStairMovement(xy, BlocksetBlockBehavior.Stair_W, BlocksetBlockBehavior.Stair_E);
                    break;
                }
                case FacingDirection.East:
                {
                    Vec2I xy = Pos.XY;
                    Pos.XY.X++;
                    ApplyStairMovement(xy, BlocksetBlockBehavior.Stair_E, BlocksetBlockBehavior.Stair_W);
                    break;
                }
                case FacingDirection.Southwest:
                {
                    Pos.XY.X--;
                    Pos.XY.Y++;
                    MovementSpeed *= DIAGONAL_MOVE_SPEED_MOD;
                    break;
                }
                case FacingDirection.Southeast:
                {
                    Pos.XY.X++;
                    Pos.XY.Y++;
                    MovementSpeed *= DIAGONAL_MOVE_SPEED_MOD;
                    break;
                }
                case FacingDirection.Northwest:
                {
                    Pos.XY.X--;
                    Pos.XY.Y--;
                    MovementSpeed *= DIAGONAL_MOVE_SPEED_MOD;
                    break;
                }
                case FacingDirection.Northeast:
                {
                    Pos.XY.X++;
                    Pos.XY.Y--;
                    MovementSpeed *= DIAGONAL_MOVE_SPEED_MOD;
                    break;
                }
            }
            Map curMap = Map;
            Vec2I curXY = Pos.XY;
            MapLayout.Block block = curMap.GetBlock_CrossMap(curXY, out Vec2I newXY, out Map newMap);
            Pos.Elevation = Overworld.GetElevationIfMovedTo(Pos.Elevation, block.Elevations);
            if (newMap == curMap)
            {
                return;
            }
            // Map crossing - Update Map, Pos, and MovingFromPos
            curMap.Objs.Remove(this);
            newMap.Objs.Add(this);
            Map = newMap;

            Pos.XY = newXY;
            MovingFromPos.XY.X += newXY.X - curXY.X;
            MovingFromPos.XY.Y += newXY.Y - curXY.Y;
            OnMapChanged(curMap, newMap);
        }

        protected virtual void OnMapChanged(Map oldMap, Map newMap) { }
        protected virtual void OnDismountFromWater() { }

        protected abstract bool CanSurf();
        protected virtual bool IsSurfing()
        {
            return false;
        }

        // TODO: Ledges, waterfall, etc
        public virtual bool Move(FacingDirection facing, bool run, bool ignoreLegalCheck)
        {
            bool surfing = IsSurfing();
            IsMovingSelf = true;
            MovementProgress = 0f;
            Facing = facing;
            MovingFromPos = Pos;
            MovingFromVisualOfs = VisualOfs;
            bool success = ignoreLegalCheck || IsMovementLegal(facing, CanSurf());
            if (success)
            {
                MovementSpeed = run ? RUN_MOVE_SPEED : WALK_MOVE_SPEED;
                ApplyMovement(facing);
                UpdateVisualProgress();
                CameraObj.Instance.CopyMovementIfAttachedTo(this); // Tell camera to move the same way
                if (surfing && !Overworld.IsSurfable(GetBlock().BlocksetBlock.Behavior))
                {
                    OnDismountFromWater();
                }
            }
            else
            {
                MovementSpeed = WALK_MOVE_SPEED * BLOCKED_MOVE_SPEED_MOD;
            }
            return success;
        }

        public virtual void Face(FacingDirection facing)
        {
            IsMovingSelf = true;
            MovementProgress = 0f;
            MovementSpeed = TURNING_MOVE_SPEED;
            Facing = facing;
            MovingFromPos = Pos;
            MovingFromVisualOfs = VisualOfs;
            UpdateVisualProgress();
        }

        private static FacingDirection GetDirectionToLook(Vec2I myXY, Vec2I otherXY)
        {
            if (otherXY.X == myXY.X)
            {
                if (otherXY.Y < myXY.Y)
                {
                    return FacingDirection.North; // x == x, y < y
                }
                return FacingDirection.South; // x == x, y == y
            }
            if (otherXY.Y == myXY.Y)
            {
                if (otherXY.X < myXY.X)
                {
                    return FacingDirection.West; // x < x, y == y
                }
                return FacingDirection.East; // x > x, y == y
            }
            if (otherXY.X < myXY.X)
            {
                if (otherXY.Y < myXY.Y)
                {
                    return FacingDirection.Northwest; // x < x, y < y
                }
                return FacingDirection.Southwest; // x < x, y > y
            }
            if (otherXY.Y < myXY.Y)
            {
                return FacingDirection.Northeast; // x > x, y < y
            }
            return FacingDirection.Southeast; // x > x, y > y
        }

        public void LookTowards(Obj other)
        {
            LookTowards(other.Pos.XY);
        }
        public void LookTowards(Vec2I otherXY)
        {
            Vec2I myXY = Pos.XY;
            if (myXY == otherXY)
            {
                return; // Same position, do nothing
            }
            Facing = GetDirectionToLook(myXY, otherXY);
        }

        private void UpdateVisualProgress()
        {
            Vec2I prevVisualOfs = ((MovingFromPos.XY - Pos.XY) * Overworld.Block_NumPixels) + MovingFromVisualOfs;
            VisualProgress = (Vec2I)Vector2.Lerp(prevVisualOfs, VisualOfs, MovementProgress);
            // TODO: (#69) check CameraObj for details
            //CameraObj.CopyMovementIfAttachedTo(this);
        }
        public void UpdateMovement()
        {
            // If movement started but not finished
            if (MovementProgress < 1f)
            {
                MovementProgress += Display.DeltaTime * MovementSpeed;
                if (MovementProgress < 1f)
                {
                    // If it's still not finished, update the visual progress and return
                    UpdateVisualProgress();
                    return;
                }
                // Finished movement just now
                MovementProgress = 1f;
                MovingFromPos = Pos;
                MovingFromVisualOfs = VisualOfs;
                UpdateVisualProgress();
            }

            // Reached here if we are not currently moving
            if (QueuedScriptMovements.Count > 0)
            {
                RunNextScriptMovement();
                return;
            }
            // TODO: Check if we should keep going for currents/waterfall/spin tiles
            IsMovingSelf = false;
            IsScriptMoving = false;
        }
    }
}
