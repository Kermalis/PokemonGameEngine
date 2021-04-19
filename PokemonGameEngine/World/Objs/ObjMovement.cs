using System;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract partial class Obj
    {
        private static bool ElevationCheck(BlocksetBlockBehavior curBehavior, BlocksetBlockBehavior targetBehavior, byte curElevation, byte targetElevations)
        {
            if (targetElevations.HasElevation(curElevation))
            {
                return true;
            }
            return Overworld.AllowsElevationChange(curBehavior) || Overworld.AllowsElevationChange(targetBehavior);
        }
        private static byte GetElevationIfMovedTo(byte curElevation, byte targetElevations)
        {
            if (!targetElevations.HasElevation(curElevation))
            {
                return targetElevations.GetLowestElevation();
            }
            return curElevation;
        }
        private bool CanMoveTo_Cardinal__CanOccupy(int targetX, int targetY,
            BlocksetBlockBehavior curBehavior, BlocksetBlockBehavior blockedTarget, bool checkElevation, byte curElevation)
        {
            // Get the x/y/map of the target block
            Map.GetXYMap(targetX, targetY, out int outX, out int outY, out Map outMap);
            Map.Layout.Block targetBlock = outMap.GetBlock_InBounds(outX, outY);
            // Check occupancy permission
            if ((targetBlock.Passage & LayoutBlockPassage.AllowOccupancy) == 0)
            {
                return false;
            }
            // Check block behaviors
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if (targetBehavior == BlocksetBlockBehavior.Surf || targetBehavior == blockedTarget)
            {
                return false;
            }
            // Check elevation
            byte targetElevations = targetBlock.Elevations;
            if (checkElevation && !ElevationCheck(curBehavior, targetBehavior, curElevation, targetElevations))
            {
                return false;
            }
            byte newElevation = GetElevationIfMovedTo(curElevation, targetElevations);
            // Check if we can pass through objs at the position
            if (CollidesWithAny_InBounds(outMap, outX, outY, newElevation))
            {
                return false;
            }
            return true;
        }
        // South/North
        private bool CanMoveTo_Cardinal(int targetX, int targetY, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget)
        {
            // Current block - return false if we are blocked
            Position p = Pos;
            Map.Layout.Block curBlock = Map.GetBlock_CrossMap(p.X, p.Y);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrent)
            {
                return false;
            }
            // Target block - return false if we are blocked
            if (!CanMoveTo_Cardinal__CanOccupy(targetX, targetY, curBehavior, blockedTarget, true, p.Elevation))
            {
                return false;
            }
            return true;
        }
        // West/East
        private bool CanMoveTo_Cardinal_ConsiderStairs(int targetX, int targetY, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget,
            BlocksetBlockBehavior upBehavior, BlocksetBlockBehavior downBehavior)
        {
            // Current block - return false if we are blocked
            Position p = Pos;
            Map.Layout.Block curBlock = Map.GetBlock_CrossMap(p.X, p.Y);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrent)
            {
                return false;
            }
            // Stairs - check if we can go up a stair that's above our target
            Map.GetXYMap(targetX, targetY - 1, out int upStairX, out int upStairY, out Map upStairMap);
            Map.Layout.Block upStairBlock = upStairMap.GetBlock_InBounds(upStairX, upStairY);
            if ((upStairBlock.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
            {
                BlocksetBlockBehavior upStairBehavior = upStairBlock.BlocksetBlock.Behavior;
                if (upStairBehavior == upBehavior)
                {
                    // Check if we can pass through objs on the position
                    byte newElevation = GetElevationIfMovedTo(p.Elevation, upStairBlock.Elevations);
                    if (CollidesWithAny_InBounds(upStairMap, upStairX, upStairY, newElevation))
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
                targetY++;
            }
            else if (curBehavior == upBehavior)
            {
                canChangeElevation = true;
            }
            // Target block - return false if we are blocked
            if (!CanMoveTo_Cardinal__CanOccupy(targetX, targetY, curBehavior, blockedTarget, !canChangeElevation, p.Elevation))
            {
                return false;
            }
            return true;
        }
        // Southwest/Southeast/Northwest/Northeast
        private bool CanMoveTo_Diagonal(int targetX, int targetY, LayoutBlockPassage neighbor1Passage, int neighbor1X, int neighbor1Y, LayoutBlockPassage neighbor2Passage, int neighbor2X, int neighbor2Y,
            BlocksetBlockBehavior blockedCurrentCardinal1, BlocksetBlockBehavior blockedCurrentCardinal2, BlocksetBlockBehavior blockedCurrentDiagonal,
            BlocksetBlockBehavior blockedTargetCardinal1, BlocksetBlockBehavior blockedTargetCardinal2, BlocksetBlockBehavior blockedTargetDiagonal,
            BlocksetBlockBehavior blockedNeighbor1, BlocksetBlockBehavior blockedNeighbor2)
        {
            // Current block - return false if we are blocked
            Position p = Pos;
            Map.Layout.Block curBlock = Map.GetBlock_CrossMap(p.X, p.Y);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrentCardinal1 || curBehavior == blockedCurrentCardinal2 || curBehavior == blockedCurrentDiagonal)
            {
                return false;
            }
            // Target block - return false if we are blocked
            Map.GetXYMap(targetX, targetY, out int targetOutX, out int targetOutY, out Map targetOutMap);
            Map.Layout.Block targetBlock = targetOutMap.GetBlock_InBounds(targetOutX, targetOutY);
            if ((targetBlock.Passage & LayoutBlockPassage.AllowOccupancy) == 0)
            {
                return false;
            }
            // Check block behaviors
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if (targetBehavior == BlocksetBlockBehavior.Surf || targetBehavior == blockedTargetCardinal1 || targetBehavior == blockedTargetCardinal2 || targetBehavior == blockedTargetDiagonal)
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
            byte newElevation = GetElevationIfMovedTo(curElevation, targetElevations);
            // Check if we can pass through objs at the position
            if (CollidesWithAny_InBounds(targetOutMap, targetOutX, targetOutY, newElevation))
            {
                return false;
            }
            // Target's neighbors - check if we can pass through them diagonally
            if (!CanPassThroughDiagonally(neighbor1X, neighbor1Y, curElevation, neighbor1Passage, blockedCurrentCardinal1, blockedTargetCardinal2, blockedNeighbor1)
                || !CanPassThroughDiagonally(neighbor2X, neighbor2Y, curElevation, neighbor2Passage, blockedTargetCardinal1, blockedCurrentCardinal2, blockedNeighbor2))
            {
                return false;
            }
            return true;
        }

        private bool CanPassThroughDiagonally(int x, int y, byte elevation, LayoutBlockPassage diagonalPassage,
            BlocksetBlockBehavior blockedCardinal1, BlocksetBlockBehavior blockedCardinal2, BlocksetBlockBehavior blockedDiagonal)
        {
            // Get the x/y/map of the block
            Map.GetXYMap(x, y, out int outX, out int outY, out Map outMap);
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
            // Check if we can pass through objs at the position (only checks current elevation, not the target elevation or any other elevations)
            if (CollidesWithAny_InBounds(outMap, outX, outY, elevation))
            {
                return false;
            }
            return true;
        }

        protected bool IsMovementLegal(FacingDirection facing)
        {
            Position p = Pos;
            int x = p.X;
            int y = p.Y;
            switch (facing)
            {
                case FacingDirection.South:
                {
                    return CanMoveTo_Cardinal(x, y + 1, BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_N);
                }
                case FacingDirection.North:
                {
                    return CanMoveTo_Cardinal(x, y - 1, BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_S);
                }
                case FacingDirection.West:
                {
                    return CanMoveTo_Cardinal_ConsiderStairs(x - 1, y, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_E,
                        BlocksetBlockBehavior.Stair_W, BlocksetBlockBehavior.Stair_E);
                }
                case FacingDirection.East:
                {
                    return CanMoveTo_Cardinal_ConsiderStairs(x + 1, y, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_W,
                        BlocksetBlockBehavior.Stair_E, BlocksetBlockBehavior.Stair_W);
                }
                case FacingDirection.Southwest:
                {
                    return CanMoveTo_Diagonal(x - 1, y + 1, LayoutBlockPassage.SoutheastPassage, x - 1, y, LayoutBlockPassage.NorthwestPassage, x, y + 1,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_SE, BlocksetBlockBehavior.Blocked_NW);
                }
                case FacingDirection.Southeast:
                {
                    return CanMoveTo_Diagonal(x + 1, y + 1, LayoutBlockPassage.SouthwestPassage, x + 1, y, LayoutBlockPassage.NortheastPassage, x, y + 1,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_SW, BlocksetBlockBehavior.Blocked_NE);
                }
                case FacingDirection.Northwest:
                {
                    return CanMoveTo_Diagonal(x - 1, y - 1, LayoutBlockPassage.NortheastPassage, x - 1, y, LayoutBlockPassage.SouthwestPassage, x, y - 1,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_NE, BlocksetBlockBehavior.Blocked_SW);
                }
                case FacingDirection.Northeast:
                {
                    return CanMoveTo_Diagonal(x + 1, y - 1, LayoutBlockPassage.NorthwestPassage, x + 1, y, LayoutBlockPassage.SoutheastPassage, x, y - 1,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_NW, BlocksetBlockBehavior.Blocked_SE);
                }
                default: throw new ArgumentOutOfRangeException(nameof(facing));
            }
        }

        private void ApplyStairMovement(Position curPos, BlocksetBlockBehavior upBehavior, BlocksetBlockBehavior downBehavior)
        {
            Map.Layout.Block curBlock = Map.GetBlock_InBounds(curPos.X, curPos.Y);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == downBehavior)
            {
                Pos.Y++;
            }
            Position newPos = Pos;
            int newX = newPos.X;
            int newY = newPos.Y;
            Map.Layout.Block upStairBlock = Map.GetBlock_CrossMap(newX, newY - 1);
            BlocksetBlockBehavior upStairBehavior = upStairBlock.BlocksetBlock.Behavior;
            if (upStairBehavior == upBehavior)
            {
                Pos.Y--;
                Pos.YOffset = StairYOffset;
                return;
            }
            Map.Layout.Block newBlock = Map.GetBlock_CrossMap(newX, newY);
            BlocksetBlockBehavior newBehavior = newBlock.BlocksetBlock.Behavior;
            if (newBehavior == downBehavior)
            {
                Pos.YOffset = StairYOffset;
            }
            else
            {
                Pos.YOffset = 0;
            }
        }
        private void ApplyMovement(FacingDirection facing)
        {
            switch (facing)
            {
                case FacingDirection.South:
                {
                    Pos.Y++;
                    break;
                }
                case FacingDirection.North:
                {
                    Pos.Y--;
                    break;
                }
                case FacingDirection.West:
                {
                    Position p = Pos;
                    Pos.X--;
                    ApplyStairMovement(p, BlocksetBlockBehavior.Stair_W, BlocksetBlockBehavior.Stair_E);
                    break;
                }
                case FacingDirection.East:
                {
                    Position p = Pos;
                    Pos.X++;
                    ApplyStairMovement(p, BlocksetBlockBehavior.Stair_E, BlocksetBlockBehavior.Stair_W);
                    break;
                }
                case FacingDirection.Southwest:
                {
                    Pos.X--;
                    Pos.Y++;
                    MovementSpeed *= DiagonalMovementSpeedModifier;
                    break;
                }
                case FacingDirection.Southeast:
                {
                    Pos.X++;
                    Pos.Y++;
                    MovementSpeed *= DiagonalMovementSpeedModifier;
                    break;
                }
                case FacingDirection.Northwest:
                {
                    Pos.X--;
                    Pos.Y--;
                    MovementSpeed *= DiagonalMovementSpeedModifier;
                    break;
                }
                case FacingDirection.Northeast:
                {
                    Pos.X++;
                    Pos.Y--;
                    MovementSpeed *= DiagonalMovementSpeedModifier;
                    break;
                }
            }
            Map.Layout.Block block = GetBlock(out Map map);
            Pos.Elevation = GetElevationIfMovedTo(Pos.Elevation, block.Elevations);
            Map curMap = Map;
            if (map == curMap)
            {
                return;
            }
            // Map crossing - Update Map, Pos, and PrevPos
            curMap.Objs.Remove(this);
            map.Objs.Add(this);
            Map = map;

            int x = Pos.X;
            int y = Pos.Y;
            Pos.X = block.X;
            Pos.Y = block.Y;
            PrevPos.X += block.X - x;
            PrevPos.Y += block.Y - y;
            OnMapChanged(curMap, map);
        }

        protected virtual void OnMapChanged(Map oldMap, Map newMap) { }

        // TODO: Ledges, waterfall, etc
        public virtual bool Move(FacingDirection facing, bool run, bool ignoreLegalCheck)
        {
            IsMovingSelf = true;
            MovementTimer = 0;
            Facing = facing;
            PrevPos = Pos;
            bool success = ignoreLegalCheck || IsMovementLegal(facing);
            if (success)
            {
                MovementSpeed = run ? RunningMovementSpeed : NormalMovementSpeed;
                ApplyMovement(facing);
                UpdateXYProgress();
                if (CameraObj.CameraAttachedTo == this)
                {
                    CameraObj.CameraCopyMovement();
                }
            }
            else
            {
                MovementSpeed = NormalMovementSpeed * BlockedMovementSpeedModifier;
            }
            return success;
        }

        public virtual void Face(FacingDirection facing)
        {
            IsMovingSelf = true;
            MovementTimer = 0;
            MovementSpeed = FaceMovementSpeed;
            Facing = facing;
            PrevPos = Pos;
            UpdateXYProgress();
        }

        private static FacingDirection GetDirectionToLook(Position myPos, Position otherPos)
        {
            if (otherPos.X == myPos.X)
            {
                if (otherPos.Y < myPos.Y)
                {
                    return FacingDirection.North;
                }
                return FacingDirection.South; // Default case if x and y are equal
            }
            else if (otherPos.Y == myPos.Y)
            {
                if (otherPos.X < myPos.X)
                {
                    return FacingDirection.West;
                }
                return FacingDirection.East;
            }
            else if (otherPos.X < myPos.X)
            {
                if (otherPos.Y < myPos.Y)
                {
                    return FacingDirection.Northwest;
                }
                return FacingDirection.Southwest;
            }
            else
            {
                if (otherPos.Y < myPos.Y)
                {
                    return FacingDirection.Northeast;
                }
                return FacingDirection.Southeast;
            }
        }

        public void LookTowards(Obj other)
        {
            LookTowards(other.Pos);
        }
        public void LookTowards(Position oPos)
        {
            Position pos = Pos;
            if (oPos.X == pos.X && oPos.Y == pos.Y)
            {
                return; // Same position, do nothing
            }
            Facing = GetDirectionToLook(pos, oPos);
        }

        private void UpdateXYProgress()
        {
            Position prevPos = PrevPos;
            Position pos = Pos;
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
            ProgressX = DoTheMath(pos.X, prevPos.X, pos.XOffset, prevPos.XOffset, Overworld.Block_NumPixelsX);
            ProgressY = DoTheMath(pos.Y, prevPos.Y, pos.YOffset, prevPos.YOffset, Overworld.Block_NumPixelsY);
        }
        public void UpdateMovement()
        {
            if (MovementTimer < 1)
            {
                MovementTimer += MovementSpeed;
                if (MovementTimer < 1)
                {
                    UpdateXYProgress();
                    return;
                }
                MovementTimer = 1;
                UpdateXYProgress();
            }

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
