using Kermalis.PokemonGameEngine.Render;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Overworld
{
    // Script movements handled in Script/ScriptMovements.cs
    internal sealed partial class Obj
    {
        public enum FacingDirection : byte
        {
            South,
            North,
            West,
            East,
            Southwest,
            Southeast,
            Northwest,
            Northeast
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
        public int X;
        public int Y;
        public byte Elevation;
        public Map Map;

        public bool CanMove = true; // Not too thought-out, so I'll probably end up removing it when scripting/waterfall/currents/spin tiles etc are implemented
        private float _movementTimer;
        private float _movementSpeed;
        private const float _faceMovementSpeed = 1 / 3f;
        private const float _normalMovementSpeed = 1 / 6f;
        private const float _runningMovementSpeed = 1 / 4f;
        private const float _diagonalMovementSpeedModifier = 0.7071067811865475f; // (2 / (sqrt((2^2) + (2^2)))
        private const float _blockedMovementSpeedModifier = 0.8f;
        private bool _leg;
        private int PrevX;
        private int PrevY;
        public int XOffset;
        public int YOffset;

        public readonly int SpriteWidth;
        public readonly int SpriteHeight;
        private readonly Sprite[] _tempSpriteSheet;

        private Obj(ushort id)
        {
            Id = id;
            LoadedObjs.Add(this);
        }
        public Obj(ushort id, string resource, int spriteWidth, int spriteHeight)
        {
            _tempSpriteSheet = RenderUtils.LoadSpriteSheet(resource, spriteWidth, spriteHeight);
            Id = id;
            SpriteWidth = spriteWidth;
            SpriteHeight = spriteHeight;
            LoadedObjs.Add(this);
        }

        private bool CanMoveTo_Cardinal(int targetX, int targetY, BlocksetBlockBehavior blockedCurrent, BlocksetBlockBehavior blockedTarget)
        {
            Map.Layout.Block curBlock = Map.GetBlock(X, Y);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrent)
            {
                return false;
            }
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
            return true;
        }
        private bool CanMoveTo_Diagonal(int targetX, int targetY, LayoutBlockPassage neighbor1Passage, int neighbor1X, int neighbor1Y, LayoutBlockPassage neighbor2Passage, int neighbor2X, int neighbor2Y,
            BlocksetBlockBehavior blockedCurrentCardinal1, BlocksetBlockBehavior blockedCurrentCardinal2, BlocksetBlockBehavior blockedCurrentDiagonal,
            BlocksetBlockBehavior blockedTargetCardinal1, BlocksetBlockBehavior blockedTargetCardinal2, BlocksetBlockBehavior blockedTargetDiagonal,
            BlocksetBlockBehavior blockedNeighbor1, BlocksetBlockBehavior blockedNeighbor2)
        {
            Map.Layout.Block curBlock = Map.GetBlock(X, Y);
            BlocksetBlockBehavior curBehavior = curBlock.BlocksetBlock.Behavior;
            if (curBehavior == blockedCurrentCardinal1 || curBehavior == blockedCurrentCardinal2 || curBehavior == blockedCurrentDiagonal)
            {
                return false;
            }
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
            Map.Layout.Block neighbor1Block = Map.GetBlock(neighbor1X, neighbor1Y);
            if ((neighbor1Block.Passage & neighbor1Passage) == 0)
            {
                return false;
            }
            BlocksetBlockBehavior neighbor1Behavior = neighbor1Block.BlocksetBlock.Behavior;
            if (neighbor1Behavior == blockedTargetCardinal2 || neighbor1Behavior == blockedCurrentCardinal1 || neighbor1Behavior == blockedNeighbor1)
            {
                return false;
            }
            Map.Layout.Block neighbor2Block = Map.GetBlock(neighbor2X, neighbor2Y);
            if ((neighbor2Block.Passage & neighbor2Passage) == 0)
            {
                return false;
            }
            BlocksetBlockBehavior neighbor2Behavior = neighbor2Block.BlocksetBlock.Behavior;
            if (neighbor2Behavior == blockedTargetCardinal1 || neighbor2Behavior == blockedCurrentCardinal2 || neighbor2Behavior == blockedNeighbor2)
            {
                return false;
            }
            return true;
        }
        private bool IsMovementLegal(FacingDirection facing)
        {
            switch (facing)
            {
                case FacingDirection.South:
                {
                    return CanMoveTo_Cardinal(X, Y + 1, BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_N);
                }
                case FacingDirection.North:
                {
                    return CanMoveTo_Cardinal(X, Y - 1, BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_S);
                }
                case FacingDirection.West:
                {
                    return CanMoveTo_Cardinal(X - 1, Y, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_E);
                }
                case FacingDirection.East:
                {
                    return CanMoveTo_Cardinal(X + 1, Y, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_W);
                }
                case FacingDirection.Southwest:
                {
                    return CanMoveTo_Diagonal(X - 1, Y + 1, LayoutBlockPassage.SoutheastPassage, X - 1, Y, LayoutBlockPassage.NorthwestPassage, X, Y + 1,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_SE, BlocksetBlockBehavior.Blocked_NW);
                }
                case FacingDirection.Southeast:
                {
                    return CanMoveTo_Diagonal(X + 1, Y + 1, LayoutBlockPassage.SouthwestPassage, X + 1, Y, LayoutBlockPassage.NortheastPassage, X, Y + 1,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_SW, BlocksetBlockBehavior.Blocked_NE);
                }
                case FacingDirection.Northwest:
                {
                    return CanMoveTo_Diagonal(X - 1, Y - 1, LayoutBlockPassage.NortheastPassage, X - 1, Y, LayoutBlockPassage.SouthwestPassage, X, Y - 1,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_NW,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_SE,
                        BlocksetBlockBehavior.Blocked_NE, BlocksetBlockBehavior.Blocked_SW);
                }
                case FacingDirection.Northeast:
                {
                    return CanMoveTo_Diagonal(X + 1, Y - 1, LayoutBlockPassage.NorthwestPassage, X + 1, Y, LayoutBlockPassage.SoutheastPassage, X, Y - 1,
                        BlocksetBlockBehavior.Blocked_N, BlocksetBlockBehavior.Blocked_E, BlocksetBlockBehavior.Blocked_NE,
                        BlocksetBlockBehavior.Blocked_S, BlocksetBlockBehavior.Blocked_W, BlocksetBlockBehavior.Blocked_SW,
                        BlocksetBlockBehavior.Blocked_NW, BlocksetBlockBehavior.Blocked_SE);
                }
                default: throw new ArgumentOutOfRangeException(nameof(facing));
            }
        }

        private void ApplyMovement(FacingDirection facing)
        {
            switch (facing)
            {
                case FacingDirection.South:
                {
                    Y++;
                    break;
                }
                case FacingDirection.North:
                {
                    Y--;
                    break;
                }
                case FacingDirection.West:
                {
                    X--;
                    break;
                }
                case FacingDirection.East:
                {
                    X++;
                    break;
                }
                case FacingDirection.Southwest:
                {
                    X--;
                    Y++;
                    _movementSpeed *= _diagonalMovementSpeedModifier;
                    break;
                }
                case FacingDirection.Southeast:
                {
                    X++;
                    Y++;
                    _movementSpeed *= _diagonalMovementSpeedModifier;
                    break;
                }
                case FacingDirection.Northwest:
                {
                    X--;
                    Y--;
                    _movementSpeed *= _diagonalMovementSpeedModifier;
                    break;
                }
                case FacingDirection.Northeast:
                {
                    X++;
                    Y--;
                    _movementSpeed *= _diagonalMovementSpeedModifier;
                    break;
                }
            }
        }

        // TODO: Elevations, map crossing, ledges
        public bool Move(FacingDirection facing, bool run, bool ignoreLegalCheck)
        {
            CanMove = false;
            _movementTimer = 1;
            _movementSpeed = run ? _runningMovementSpeed : _normalMovementSpeed;
            _leg = !_leg;
            Facing = facing;
            PrevX = X;
            PrevY = Y;
            bool success = ignoreLegalCheck || IsMovementLegal(facing);
            if (success)
            {
                ApplyMovement(facing);
                UpdateXYOffsets();
                if (CameraAttachedTo == this)
                {
                    CameraCopyMovement();
                }
            }
            else
            {
                _movementSpeed *= _blockedMovementSpeedModifier;
            }
            return success;
        }

        public void Face(FacingDirection facing)
        {
            CanMove = false;
            _movementTimer = 1;
            _movementSpeed = _faceMovementSpeed;
            _leg = !_leg;
            Facing = facing;
            PrevX = X;
            PrevY = Y;
            UpdateXYOffsets();
        }

        private void UpdateXYOffsets()
        {
            float t = _movementTimer;
            XOffset = (int)(t * (PrevX - X) * 16);
            YOffset = (int)(t * (PrevY - Y) * 16);
        }
        public void UpdateMovementTimer()
        {
            if (_movementTimer <= 0)
            {
                return;
            }
            _movementTimer -= _movementSpeed;
            if (_movementTimer <= 0)
            {
                _movementTimer = 0;
                UpdateXYOffsets();
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
            UpdateXYOffsets();
        }
        public static void CameraCopyMovement()
        {
            Obj c = Camera;
            Obj other = CameraAttachedTo;
            c.CanMove = other.CanMove;
            c._movementTimer = other._movementTimer;
            c._movementSpeed = other._movementSpeed;
            c._leg = other._leg;
            c.X = CameraOfsX + other.X;
            c.Y = CameraOfsY + other.Y;
            c.PrevX = CameraOfsX + other.PrevX;
            c.PrevY = CameraOfsY + other.PrevY;
            c.XOffset = other.XOffset;
            c.YOffset = other.YOffset;
        }
        public void CopyMovement(Obj other)
        {
            CanMove = other.CanMove;
            _movementTimer = other._movementTimer;
            _movementSpeed = other._movementSpeed;
            _leg = other._leg;
            X = other.X;
            Y = other.Y;
            PrevX = other.PrevX;
            PrevY = other.PrevY;
            XOffset = other.XOffset;
            YOffset = other.YOffset;
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

        // TODO: Shadows, reflections
        public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
        {
            bool ShowLegs()
            {
                float t = _movementTimer;
                return t != 0 && t <= 0.6f;
            }
            byte f = (byte)Facing;
            int spriteNum = ShowLegs() ? (_leg ? f + 8 : f + 16) : f; // TODO: Fall-back to specific sprites if the target sprite doesn't exist
            _tempSpriteSheet[spriteNum].DrawOn(bmpAddress, bmpWidth, bmpHeight, x, y);
        }
    }
}
