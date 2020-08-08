using System;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract partial class Obj
    {
        private bool ElevationCheck(BlocksetBlockBehavior curBehavior, BlocksetBlockBehavior targetBehavior, byte curElevation, byte targetElevation)
        {
            if (curElevation == targetElevation)
            {
                return true;
            }
            return Overworld.AllowsElevationChange(curBehavior) || Overworld.AllowsElevationChange(targetBehavior);
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
            if (checkElevation && !ElevationCheck(curBehavior, targetBehavior, curElevation, targetBlock.Elevation))
            {
                return false;
            }
            // Check if we can pass through objs at the position
            if (CollidesWithAny_InBounds(outMap, outX, outY))
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
                    if (CollidesWithAny_InBounds(upStairMap, upStairX, upStairY))
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
            if (!ElevationCheck(curBehavior, targetBehavior, p.Elevation, targetBlock.Elevation))
            {
                return false;
            }
            // Check if we can pass through objs at the position
            if (CollidesWithAny_InBounds(targetOutMap, targetOutX, targetOutY))
            {
                return false;
            }
            // Target's neighbors - check if we can pass through them diagonally
            if (!CanPassThroughDiagonally(neighbor1X, neighbor1Y, neighbor1Passage, blockedTargetCardinal2, blockedTargetCardinal1, blockedNeighbor1)
                || !CanPassThroughDiagonally(neighbor2X, neighbor2Y, neighbor2Passage, blockedTargetCardinal1, blockedTargetCardinal2, blockedNeighbor2))
            {
                return false;
            }
            return true;
        }

        private bool CanPassThroughDiagonally(int x, int y, LayoutBlockPassage diagonalPassage,
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
            // Check if we can pass through objs at the position
            if (CollidesWithAny_InBounds(outMap, outX, outY))
            {
                return false;
            }
            return true;
        }

        private bool IsMovementLegal(FacingDirection facing)
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
                    _movementSpeed *= DiagonalMovementSpeedModifier;
                    break;
                }
                case FacingDirection.Southeast:
                {
                    Pos.X++;
                    Pos.Y++;
                    _movementSpeed *= DiagonalMovementSpeedModifier;
                    break;
                }
                case FacingDirection.Northwest:
                {
                    Pos.X--;
                    Pos.Y--;
                    _movementSpeed *= DiagonalMovementSpeedModifier;
                    break;
                }
                case FacingDirection.Northeast:
                {
                    Pos.X++;
                    Pos.Y--;
                    _movementSpeed *= DiagonalMovementSpeedModifier;
                    break;
                }
            }
            Map.Layout.Block block = GetBlock(out Map map);
            Pos.Elevation = block.Elevation;
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
        }

        // TODO: Ledges, waterfall, etc
        public virtual bool Move(FacingDirection facing, bool run, bool ignoreLegalCheck)
        {
            CanMove = false;
            _movementTimer = 0;
            _movementSpeed = run ? RunningMovementSpeed : NormalMovementSpeed;
            Facing = facing;
            PrevPos = Pos;
            bool success = ignoreLegalCheck || IsMovementLegal(facing);
            if (success)
            {
                ApplyMovement(facing);
                UpdateXYProgress();
                if (CameraObj.CameraAttachedTo == this)
                {
                    CameraObj.CameraCopyMovement();
                }
            }
            else
            {
                _movementSpeed *= BlockedMovementSpeedModifier;
            }
            return success;
        }

        public virtual void Face(FacingDirection facing)
        {
            CanMove = false;
            _movementTimer = 0;
            _movementSpeed = FaceMovementSpeed;
            Facing = facing;
            PrevPos = Pos;
            UpdateXYProgress();
        }

        private void UpdateXYProgress()
        {
            Position prevPos = PrevPos;
            Position pos = Pos;
            float t = _movementTimer; // Goes from 0% to 100%
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
            _progressX = DoTheMath(pos.X, prevPos.X, pos.XOffset, prevPos.XOffset, Overworld.Block_NumPixelsX);
            _progressY = DoTheMath(pos.Y, prevPos.Y, pos.YOffset, prevPos.YOffset, Overworld.Block_NumPixelsY);
        }
        public void UpdateMovementTimer()
        {
            if (_movementTimer >= 1)
            {
                return;
            }
            _movementTimer += _movementSpeed;
            if (_movementTimer >= 1)
            {
                _movementTimer = 1;
                UpdateXYProgress();
                // Check if we have queued movements
                if (QueuedScriptMovements.Count > 0)
                {
                    RunNextScriptMovement();
                    return;
                }
                // TODO: Check if we should keep going for currents/waterfall/spin tiles
                CanMove = true;
                return;
            }
            UpdateXYProgress();
        }
        public virtual void CopyMovement(Obj other)
        {
            CanMove = other.CanMove;
            _movementTimer = other._movementTimer;
            _movementSpeed = other._movementSpeed;
            Pos = other.Pos;
            PrevPos = other.PrevPos;
            _progressX = other._progressX;
            _progressY = other._progressY;
            UpdateMap(other.Map);
        }
    }
}
