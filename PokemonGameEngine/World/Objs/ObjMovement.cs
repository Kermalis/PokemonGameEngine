using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World.Maps;
using System;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract partial class Obj
    {
        // Move speed constants represent 1/x seconds to complete the movement
        protected const float TURNING_MOVE_SPEED = 5f;
        protected const float WALK_MOVE_SPEED = 4f;
        protected const float RUN_MOVE_SPEED = 6f;
        protected const float DIAGONAL_MOVE_SPEED_MOD = 0.7071067811865475f; // (2 / (sqrt((2^2) + (2^2)))
        protected const float BLOCKED_MOVE_SPEED_MOD = 0.8f;
        protected const int STAIR_Y_OFFSET = +6; // Any offset will work

        private static bool ElevationCheck(BlocksetBlockBehavior curBehavior, BlocksetBlockBehavior targetBehavior, byte curElevation, byte targetElevations)
        {
            if (targetElevations.HasElevation(curElevation))
            {
                return true;
            }
            return Overworld.AllowsElevationChange(curBehavior) || Overworld.AllowsElevationChange(targetBehavior);
        }
        private bool CanMoveTo_Cardinal__CanOccupy(Pos2D targetXY, bool allowSurf,
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
            byte newElevation = Overworld.GetElevationIfMovedTo(curElevation, targetElevations);
            // Check if we can pass through objs at the position
            if (CollidesWithAny_InBounds(targetMap, new WorldPos(targetXY, newElevation)))
            {
                return false;
            }
            return true;
        }
        // South/North
        private bool CanMoveTo_Cardinal(Pos2D targetXY, bool allowSurf, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget)
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
        private bool CanMoveTo_Cardinal_ConsiderStairs(Pos2D targetXY, bool allowSurf, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget,
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
            Map.GetXYMap(targetXY.North(), out Pos2D upStairPos, out Map upStairMap);
            MapLayout.Block upStairBlock = upStairMap.GetBlock_InBounds(upStairPos);
            if ((upStairBlock.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
            {
                BlocksetBlockBehavior upStairBehavior = upStairBlock.BlocksetBlock.Behavior;
                if (upStairBehavior == upBehavior)
                {
                    // Check if we can pass through objs on the position
                    byte newElevation = Overworld.GetElevationIfMovedTo(p.Elevation, upStairBlock.Elevations);
                    if (CollidesWithAny_InBounds(upStairMap, new WorldPos(upStairPos, newElevation)))
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
        private bool CanMoveTo_Diagonal(Pos2D targetXY, bool allowSurf, LayoutBlockPassage neighbor1Passage, Pos2D neighbor1XY, LayoutBlockPassage neighbor2Passage, Pos2D neighbor2XY,
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
            byte newElevation = Overworld.GetElevationIfMovedTo(curElevation, targetElevations);
            // Check if we can pass through objs at the position
            if (CollidesWithAny_InBounds(targetMap, new WorldPos(targetXY, newElevation)))
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
            Map.GetXYMap(pos.XY, out Pos2D targetXY, out Map targetMap);
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
            // Check if we can pass through objs at the position (only checks current elevation, not the target elevation or any other elevations)
            if (CollidesWithAny_InBounds(targetMap, new WorldPos(targetXY, pos.Elevation)))
            {
                return false;
            }
            return true;
        }

        public bool IsMovementLegal(FacingDirection facing, bool allowSurf)
        {
            Pos2D xy = Pos.XY;
            switch (facing)
            {
                case FacingDirection.South:
                {
                    return CanMoveTo_Cardinal(xy.South(), allowSurf, BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_N);
                }
                case FacingDirection.North:
                {
                    return CanMoveTo_Cardinal(xy.North(), allowSurf, BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_S);
                }
                case FacingDirection.West:
                {
                    return CanMoveTo_Cardinal_ConsiderStairs(xy.West(), allowSurf, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_E,
                        BlocksetBlockBehavior.Stair_W, BlocksetBlockBehavior.Stair_E);
                }
                case FacingDirection.East:
                {
                    return CanMoveTo_Cardinal_ConsiderStairs(xy.East(), allowSurf, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_W,
                        BlocksetBlockBehavior.Stair_E, BlocksetBlockBehavior.Stair_W);
                }
                case FacingDirection.Southwest:
                {
                    return CanMoveTo_Diagonal(xy.Southwest(), allowSurf, LayoutBlockPassage.SoutheastPassage, xy.West(), LayoutBlockPassage.NorthwestPassage, xy.South(),
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_SE, BlocksetBlockBehavior.Blocked_NW);
                }
                case FacingDirection.Southeast:
                {
                    return CanMoveTo_Diagonal(xy.Southeast(), allowSurf, LayoutBlockPassage.SouthwestPassage, xy.East(), LayoutBlockPassage.NortheastPassage, xy.South(),
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_SW, BlocksetBlockBehavior.Blocked_NE);
                }
                case FacingDirection.Northwest:
                {
                    return CanMoveTo_Diagonal(xy.Northwest(), allowSurf, LayoutBlockPassage.NortheastPassage, xy.West(), LayoutBlockPassage.SouthwestPassage, xy.North(),
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_NE, BlocksetBlockBehavior.Blocked_SW);
                }
                case FacingDirection.Northeast:
                {
                    return CanMoveTo_Diagonal(xy.Northeast(), allowSurf, LayoutBlockPassage.NorthwestPassage, xy.East(), LayoutBlockPassage.SoutheastPassage, xy.North(),
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_NW, BlocksetBlockBehavior.Blocked_SE);
                }
                default: throw new ArgumentOutOfRangeException(nameof(facing));
            }
        }

        private void ApplyStairMovement(Pos2D curXY, BlocksetBlockBehavior upBehavior, BlocksetBlockBehavior downBehavior)
        {
            MapLayout.Block curBlock = Map.GetBlock_InBounds(curXY);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == downBehavior)
            {
                Pos.XY.Y++;
            }
            Pos2D newXY = Pos.XY;
            MapLayout.Block upStairBlock = Map.GetBlock_CrossMap(newXY.North(), out _, out _);
            BlocksetBlockBehavior upStairBehavior = upStairBlock.BlocksetBlock.Behavior;
            if (upStairBehavior == upBehavior)
            {
                Pos.XY.Y--;
                VisualOffset.Y = STAIR_Y_OFFSET;
                return;
            }
            MapLayout.Block newBlock = Map.GetBlock_CrossMap(newXY, out _, out _);
            BlocksetBlockBehavior newBehavior = newBlock.BlocksetBlock.Behavior;
            if (newBehavior == downBehavior)
            {
                VisualOffset.Y = STAIR_Y_OFFSET;
            }
            else
            {
                VisualOffset.Y = 0;
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
                    Pos2D xy = Pos.XY;
                    Pos.XY.X--;
                    ApplyStairMovement(xy, BlocksetBlockBehavior.Stair_W, BlocksetBlockBehavior.Stair_E);
                    break;
                }
                case FacingDirection.East:
                {
                    Pos2D xy = Pos.XY;
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
            Pos2D curXY = Pos.XY;
            MapLayout.Block block = curMap.GetBlock_CrossMap(curXY, out Pos2D newXY, out Map newMap);
            Pos.Elevation = Overworld.GetElevationIfMovedTo(Pos.Elevation, block.Elevations);
            if (newMap == curMap)
            {
                return;
            }
            // Map crossing - Update Map, Pos, and PrevPos
            curMap.Objs.Remove(this);
            newMap.Objs.Add(this);
            Map = newMap;

            Pos.XY = newXY;
            PrevPos.XY.X += newXY.X - curXY.X;
            PrevPos.XY.Y += newXY.Y - curXY.Y;
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
            MovementTimer = 0f;
            Facing = facing;
            PrevPos = Pos;
            PrevVisualOffset = VisualOffset;
            bool success = ignoreLegalCheck || IsMovementLegal(facing, CanSurf());
            if (success)
            {
                MovementSpeed = run ? RUN_MOVE_SPEED : WALK_MOVE_SPEED;
                ApplyMovement(facing);
                UpdateVisualProgress();
                CameraObj.CopyMovementIfAttachedTo(this); // Tell camera to move the same way
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
            MovementTimer = 0;
            MovementSpeed = TURNING_MOVE_SPEED;
            Facing = facing;
            PrevPos = Pos;
            PrevVisualOffset = VisualOffset;
            UpdateVisualProgress();
        }

        private static FacingDirection GetDirectionToLook(Pos2D myXY, Pos2D otherXY)
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
        public void LookTowards(Pos2D otherXY)
        {
            Pos2D myXY = Pos.XY;
            if (myXY.Equals(otherXY))
            {
                return; // Same position, do nothing
            }
            Facing = GetDirectionToLook(myXY, otherXY);
        }

        private void UpdateVisualProgress()
        {
            Pos2D prevXY = PrevPos.XY;
            Pos2D prevOfs = PrevVisualOffset;
            Pos2D xy = Pos.XY;
            Pos2D ofs = VisualOffset;
            float t = MovementTimer; // Goes from 0% to 100%
            int DoTheMath(int cur, int prev, int curOfs, int prevOfs, int numPixelsInBlock)
            {
                int blockDiff = (prev - cur) * numPixelsInBlock;
                int prevVisualOfs = blockDiff + prevOfs;
                // If we are going from 6 to -10, visualOfsScale would be -16
                // If we are going from 6 to  00, visualOfsScale would be -06
                int visualOfsScale = curOfs - prevVisualOfs;
                // Scale from previous value to new value based on % of transition
                return (int)(prevVisualOfs + (t * visualOfsScale));
            }
            VisualProgress.X = DoTheMath(xy.X, prevXY.X, ofs.X, prevOfs.X, Overworld.Block_NumPixelsX);
            VisualProgress.Y = DoTheMath(xy.Y, prevXY.Y, ofs.Y, prevOfs.Y, Overworld.Block_NumPixelsY);
            // TODO: (#69) check CameraObj for details
            //CameraObj.CopyMovementIfAttachedTo(this);
        }
        public void UpdateMovement()
        {
            // If movement started but not finished
            if (MovementTimer < 1)
            {
                MovementTimer += Display.DeltaTime * MovementSpeed;
                if (MovementTimer < 1)
                {
                    // If it's still not finished, update the visual progress and return
                    UpdateVisualProgress();
                    return;
                }
                // Finished movement just now
                MovementTimer = 1;
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
            IsScriptMoving = QueuedScriptMovements.Count != 0;
        }
    }
}
