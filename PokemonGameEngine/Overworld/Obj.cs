using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Scripts;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Overworld
{
    // Script movements handled in Script/ScriptMovements.cs
    internal sealed partial class Obj
    {
        public struct Position
        {
            public int X;
            public int Y;
            public byte Elevation;
            public int XOffset;
            public int YOffset;
        }

        public static readonly List<Obj> LoadedObjs = new List<Obj>();

        public const ushort PlayerId = ushort.MaxValue;
        public const ushort CameraId = PlayerId - 1;

        public static readonly Obj Player = new Obj(PlayerId, "TestNPC.png", 32, 32);
        public static readonly Obj Camera = new Obj(CameraId);
        public static int CameraOfsX;
        public static int CameraOfsY;
        public static Obj CameraAttachedTo = Player;

        public readonly ushort Id;

        public FacingDirection Facing;
        public Position Pos;
        public Position PrevPos;
        public Map Map;

        public bool CanMove = true; // Not too thought-out, so I'll probably end up removing it when scripting/waterfall/currents/spin tiles etc are implemented
        private float _movementTimer = 1;
        private float _movementSpeed;
        private const float FaceMovementSpeed = 1 / 3f;
        private const float NormalMovementSpeed = 1 / 6f;
        private const float RunningMovementSpeed = 1 / 4f;
        private const float DiagonalMovementSpeedModifier = 0.7071067811865475f; // (2 / (sqrt((2^2) + (2^2)))
        private const float BlockedMovementSpeedModifier = 0.8f;
        private const int StairYOffset = 6; // Any offset will work
        private bool _leg;
        public int ProgressX;
        public int ProgressY;

        public readonly int SpriteWidth;
        public readonly int SpriteHeight;
        private readonly Sprite[] _tempSpriteSheet;

        private Obj(ushort id)
        {
            Id = id;
            Pos = new Position();
            PrevPos = new Position();
            LoadedObjs.Add(this);
        }
        public Obj(ushort id, string resource, int spriteWidth, int spriteHeight)
        {
            _tempSpriteSheet = RenderUtils.LoadSpriteSheet(resource, spriteWidth, spriteHeight);
            Id = id;
            Pos = new Position();
            PrevPos = new Position();
            SpriteWidth = spriteWidth;
            SpriteHeight = spriteHeight;
            LoadedObjs.Add(this);
        }

        // South/North
        private bool CanMoveTo_Cardinal(int targetX, int targetY, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget)
        {
            // Current block - return false if we are blocked
            Position p = Pos;
            Map.Layout.Block curBlock = Map.GetBlock(p.X, p.Y);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrent)
            {
                return false;
            }
            // Target block - return false if we are blocked
            Map.Layout.Block targetBlock = Map.GetBlock(targetX, targetY);
            if ((targetBlock.Passage & LayoutBlockPassage.AllowOccupancy) == 0)
            {
                return false;
            }
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if (targetBehavior == BlocksetBlockBehavior.Surf || targetBehavior == blockedTarget)
            {
                return false;
            }
            bool canChangeElevation = curBehavior == BlocksetBlockBehavior.AllowElevationChange || targetBehavior == BlocksetBlockBehavior.AllowElevationChange;
            if (!canChangeElevation && targetBlock.Elevation != p.Elevation)
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
            Map.Layout.Block curBlock = Map.GetBlock(p.X, p.Y);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrent)
            {
                return false;
            }
            // Stairs - return true if we can go up a stair that's above our target
            Map.Layout.Block upStairBlock = Map.GetBlock(targetX, targetY - 1);
            if ((upStairBlock.Passage & LayoutBlockPassage.AllowOccupancy) != 0)
            {
                BlocksetBlockBehavior upStairBehavior = upStairBlock.BlocksetBlock.Behavior;
                if (upStairBehavior == upBehavior)
                {
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
            Map.Layout.Block targetBlock = Map.GetBlock(targetX, targetY);
            if ((targetBlock.Passage & LayoutBlockPassage.AllowOccupancy) == 0)
            {
                return false;
            }
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if (targetBehavior == BlocksetBlockBehavior.Surf || targetBehavior == blockedTarget)
            {
                return false;
            }
            if (!canChangeElevation)
            {
                canChangeElevation = curBehavior == BlocksetBlockBehavior.AllowElevationChange || targetBehavior == BlocksetBlockBehavior.AllowElevationChange;
                if (!canChangeElevation && targetBlock.Elevation != p.Elevation)
                {
                    return false;
                }
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
            Map.Layout.Block curBlock = Map.GetBlock(p.X, p.Y);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrentCardinal1 || curBehavior == blockedCurrentCardinal2 || curBehavior == blockedCurrentDiagonal)
            {
                return false;
            }
            // Target block - return false if we are blocked
            Map.Layout.Block targetBlock = Map.GetBlock(targetX, targetY);
            if ((targetBlock.Passage & LayoutBlockPassage.AllowOccupancy) == 0)
            {
                return false;
            }
            BlocksetBlockBehavior targetBehavior = targetBlock.BlocksetBlock.Behavior;
            if (targetBehavior == BlocksetBlockBehavior.Surf || targetBehavior == blockedTargetCardinal1 || targetBehavior == blockedTargetCardinal2 || targetBehavior == blockedTargetDiagonal)
            {
                return false;
            }
            // Target's neighbors - return false if we cannot pass diagonally through them
            if (!CanPassThroughDiagonally(neighbor1X, neighbor1Y, neighbor1Passage, blockedTargetCardinal2, blockedTargetCardinal1, blockedNeighbor1)
                || !CanPassThroughDiagonally(neighbor2X, neighbor2Y, neighbor2Passage, blockedTargetCardinal1, blockedTargetCardinal2, blockedNeighbor2))
            {
                return false;
            }
            bool canChangeElevation = curBehavior == BlocksetBlockBehavior.AllowElevationChange || targetBehavior == BlocksetBlockBehavior.AllowElevationChange;
            if (!canChangeElevation && targetBlock.Elevation != p.Elevation)
            {
                return false;
            }
            return true;
        }

        private bool CanPassThroughDiagonally(int x, int y, LayoutBlockPassage diagonalPassage,
            BlocksetBlockBehavior blockedCardinal1, BlocksetBlockBehavior blockedCardinal2, BlocksetBlockBehavior blockedDiagonal)
        {
            Map.Layout.Block block = Map.GetBlock(x, y);
            if ((block.Passage & diagonalPassage) == 0)
            {
                return false;
            }
            BlocksetBlockBehavior blockBehavior = block.BlocksetBlock.Behavior;
            if (blockBehavior == blockedCardinal1 || blockBehavior == blockedCardinal2 || blockBehavior == blockedDiagonal)
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
            Map.Layout.Block curBlock = Map.GetBlock(curPos.X, curPos.Y);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == downBehavior)
            {
                Pos.Y++;
            }
            Position newPos = Pos;
            int newX = newPos.X;
            int newY = newPos.Y;
            Map.Layout.Block upStairBlock = Map.GetBlock(newX, newY - 1);
            BlocksetBlockBehavior upStairBehavior = upStairBlock.BlocksetBlock.Behavior;
            if (upStairBehavior == upBehavior)
            {
                Pos.Y--;
                Pos.YOffset = StairYOffset;
                return;
            }
            Map.Layout.Block newBlock = Map.GetBlock(newX, newY);
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
            Pos.Elevation = GetBlock().Elevation;
        }

        // TODO: Map crossing, ledges, etc
        public bool Move(FacingDirection facing, bool run, bool ignoreLegalCheck)
        {
            CanMove = false;
            _movementTimer = 0;
            _movementSpeed = run ? RunningMovementSpeed : NormalMovementSpeed;
            _leg = !_leg;
            Facing = facing;
            PrevPos = Pos;
            bool success = ignoreLegalCheck || IsMovementLegal(facing);
            if (success)
            {
                ApplyMovement(facing);
                UpdateXYProgress();
                if (CameraAttachedTo == this)
                {
                    CameraCopyMovement();
                }
            }
            else
            {
                _movementSpeed *= BlockedMovementSpeedModifier;
            }
            return success;
        }

        public void Face(FacingDirection facing)
        {
            CanMove = false;
            _movementTimer = 0;
            _movementSpeed = FaceMovementSpeed;
            _leg = !_leg;
            Facing = facing;
            PrevPos = Pos;
            UpdateXYProgress();
        }

        private void UpdateXYProgress()
        {
            Position prevPos = PrevPos;
            Position pos = Pos;
            float t = _movementTimer; // Goes from 0% to 100%
            int DoTheMath(int cur, int prev, int curOfs, int prevOfs)
            {
                int blockDiff = (prev - cur) * 16;
                int prevVisualOfs = blockDiff + prevOfs;
                // If we are going from 6 to -10, visualOfsScale would be -16
                // If we are going from 6 to  00, visualOfsScale would be -06
                int visualOfsScale = curOfs - prevVisualOfs;
                // Scale from previous value to new value based on % of transition
                return (int)(prevVisualOfs + (t * visualOfsScale));
            }
            ProgressX = DoTheMath(pos.X, PrevPos.X, pos.XOffset, prevPos.XOffset);
            ProgressY = DoTheMath(pos.Y, PrevPos.Y, pos.YOffset, PrevPos.YOffset);
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
        public static void CameraCopyMovement()
        {
            Obj c = Camera;
            Obj other = CameraAttachedTo;
            c.CopyMovement(other);
        }
        public void CopyMovement(Obj other)
        {
            CanMove = other.CanMove;
            _movementTimer = other._movementTimer;
            _movementSpeed = other._movementSpeed;
            _leg = other._leg;
            Pos = other.Pos;
            PrevPos = other.PrevPos;
            ProgressX = other.ProgressX;
            ProgressY = other.ProgressY;
            Map.Objs.Remove(this);
            Map = other.Map;
            Map.Objs.Add(this);
        }

        public static Obj GetObj(ushort id)
        {
            foreach (Obj o in LoadedObjs)
            {
                if (o.Id == id)
                {
                    return o;
                }
            }
            return null;
        }

        public Map.Layout.Block GetBlock()
        {
            Position p = Pos;
            return Map.GetBlock(p.X, p.Y);
        }

        public void Warp(IWarp warp)
        {
            Map.Objs.Remove(this);
            var map = Map.LoadOrGet(warp.DestMapId);
            int x = warp.DestX;
            int y = warp.DestY;
            Map = map;
            map.Objs.Add(this);
            Pos.X = x;
            Pos.Y = y;
            Pos.Elevation = warp.DestElevation;
            PrevPos = Pos;
            Map.Layout.Block block = map.GetBlock(x, y);
            // Facing is of the original direction unless the block behavior says otherwise
            // All QueuedScriptMovements will be run after the warp is complete
            if (block.BlocksetBlock.Behavior == BlocksetBlockBehavior.Warp_WalkSouthOnExit)
            {
                Facing = FacingDirection.South;
                QueuedScriptMovements.Enqueue(ScriptMovement.Walk_S);
            }
            if (CameraAttachedTo == this)
            {
                CameraCopyMovement();
            }
        }

        // TODO: Shadows, reflections
        public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
        {
            bool ShowLegs()
            {
                float t = _movementTimer;
                return t != 1 && t >= 0.6f;
            }
            byte f = (byte)Facing;
            int spriteNum = ShowLegs() ? (_leg ? f + 8 : f + 16) : f; // TODO: Fall-back to specific sprites if the target sprite doesn't exist
            _tempSpriteSheet[spriteNum].DrawOn(bmpAddress, bmpWidth, bmpHeight, x, y);
        }
    }
}
