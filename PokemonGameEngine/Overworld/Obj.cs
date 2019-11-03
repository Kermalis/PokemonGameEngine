namespace Kermalis.PokemonGameEngine.Overworld
{
    internal class Obj
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

        public const ushort PlayerId = ushort.MaxValue;
        public const ushort CameraId = PlayerId - 1;

        public static readonly Obj Camera = new Obj(CameraId);

        public readonly ushort Id;

        public FacingDirection Facing;
        public int X;
        public int Y;
        public Map Map;

        public float MovementTimer;
        private float _movementSpeed; // TODO: Framerate
        private const float _diagonalMovementSpeedModifier = 0.7071067811865475f;
        private bool _isMoving;
        protected bool _leg;
        public int XOffset;
        public int YOffset;

        protected Obj(ushort id)
        {
            Id = id;
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
    }
}
