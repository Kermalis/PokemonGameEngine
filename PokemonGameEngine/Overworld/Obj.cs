using Kermalis.PokemonGameEngine.Util;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class Obj
    {
        public enum FacingDirection : byte
        {
            South,
            North,
            West,
            East,
            SouthWest,
            SouthEast,
            NorthWest,
            NorthEast
        }

        public static readonly List<Obj> LoadedObjs = new List<Obj>();

        public const ushort PlayerId = ushort.MaxValue;
        public const ushort CameraId = PlayerId - 1;

        public static readonly Obj Player = new Obj(PlayerId, "TestNPC.png", 32, 32);
        public static readonly Obj Camera = new Obj(CameraId);

        public readonly ushort Id;

        public FacingDirection Facing;
        public int X;
        public int Y;
        public byte Elevation;
        public Map Map;

        public float MovementTimer;
        private float _movementSpeed; // TODO: Framerate
        private const float _diagonalMovementSpeedModifier = 0.7071067811865475f; // (2 / (sqrt((2^2) + (2^2)))
        private bool _isMoving;
        private bool _leg;
        public int XOffset;
        public int YOffset;

        public readonly int SpriteWidth;
        public readonly int SpriteHeight;
        private readonly uint[][][] _tempSpriteSheet;

        private Obj(ushort id)
        {
            Id = id;
            LoadedObjs.Add(this);
        }
        public Obj(ushort id, string resource, int spriteWidth, int spriteHeight)
        {
            _tempSpriteSheet = RenderUtil.LoadSpriteSheet(resource, spriteWidth, spriteHeight);
            Id = id;
            SpriteWidth = spriteWidth;
            SpriteHeight = spriteHeight;
            LoadedObjs.Add(this);
        }

        public void Move(FacingDirection facing, bool run)
        {
            MovementTimer = 1;
            _movementSpeed = run ? 0.25f : 0.1f;
            _leg = !_leg;
            Facing = facing;
            // TODO: Collisions, map crossing
            switch (facing)
            {
                case FacingDirection.South:
                {
                    _isMoving = true;
                    Y++;
                    YOffset = -15;
                    break;
                }
                case FacingDirection.North:
                {
                    _isMoving = true;
                    Y--;
                    YOffset = 15;
                    break;
                }
                case FacingDirection.West:
                {
                    _isMoving = true;
                    X--;
                    XOffset = 15;
                    break;
                }
                case FacingDirection.East:
                {
                    _isMoving = true;
                    X++;
                    XOffset = -15;
                    break;
                }
                case FacingDirection.SouthWest:
                {
                    _movementSpeed *= _diagonalMovementSpeedModifier;
                    _isMoving = true;
                    X--;
                    Y++;
                    XOffset = 15;
                    YOffset = -15;
                    break;
                }
                case FacingDirection.SouthEast:
                {
                    _movementSpeed *= _diagonalMovementSpeedModifier;
                    _isMoving = true;
                    X++;
                    Y++;
                    XOffset = -15;
                    YOffset = -15;
                    break;
                }
                case FacingDirection.NorthWest:
                {
                    _movementSpeed *= _diagonalMovementSpeedModifier;
                    _isMoving = true;
                    X--;
                    Y--;
                    XOffset = 15;
                    YOffset = 15;
                    break;
                }
                case FacingDirection.NorthEast:
                {
                    _movementSpeed *= _diagonalMovementSpeedModifier;
                    _isMoving = true;
                    X++;
                    Y--;
                    XOffset = -15;
                    YOffset = 15;
                    break;
                }
            }
        }
        public void UpdateMovement()
        {
            if (MovementTimer > 0)
            {
                MovementTimer -= _movementSpeed;
                if (MovementTimer < 0)
                {
                    MovementTimer = 0;
                }
            }
            if (_isMoving)
            {
                switch (Facing)
                {
                    case FacingDirection.South:
                    {
                        YOffset = (int)(MovementTimer * -15);
                        break;
                    }
                    case FacingDirection.North:
                    {
                        YOffset = (int)(MovementTimer * 15);
                        break;
                    }
                    case FacingDirection.West:
                    {
                        XOffset = (int)(MovementTimer * 15);
                        break;
                    }
                    case FacingDirection.East:
                    {
                        XOffset = (int)(MovementTimer * -15);
                        break;
                    }
                    case FacingDirection.SouthWest:
                    {
                        XOffset = (int)(MovementTimer * 15);
                        YOffset = (int)(MovementTimer * -15);
                        break;
                    }
                    case FacingDirection.SouthEast:
                    {
                        XOffset = (int)(MovementTimer * -15);
                        YOffset = (int)(MovementTimer * -15);
                        break;
                    }
                    case FacingDirection.NorthWest:
                    {
                        XOffset = (int)(MovementTimer * 15);
                        YOffset = (int)(MovementTimer * 15);
                        break;
                    }
                    case FacingDirection.NorthEast:
                    {
                        XOffset = (int)(MovementTimer * -15);
                        YOffset = (int)(MovementTimer * 15);
                        break;
                    }
                }
            }
        }

        // TODO: Shadows, reflections
        public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
        {
            bool ShowLegs()
            {
                return MovementTimer != 0 && MovementTimer <= 0.6f;
            }
            byte f = (byte)Facing;
            int spriteNum = ShowLegs() ? (_leg ? f + 8 : f + 16) : f; // TODO: Fall-back to specific sprites if the target sprite doesn't exist
            RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, x, y, _tempSpriteSheet[spriteNum], false, false);
        }
    }
}
