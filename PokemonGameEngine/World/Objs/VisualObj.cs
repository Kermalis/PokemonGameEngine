using Kermalis.PokemonGameEngine.Render;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract class VisualObj : Obj
    {
        private bool _leg;

        public readonly int SpriteWidth;
        public readonly int SpriteHeight;
        private readonly Sprite[] _tempSpriteSheet;

        protected VisualObj(ushort id, string resource, int spriteWidth, int spriteHeight)
            : base(id)
        {
            _tempSpriteSheet = RenderUtils.LoadSpriteSheet(resource, spriteWidth, spriteHeight);
            SpriteWidth = spriteWidth;
            SpriteHeight = spriteHeight;
        }
        protected VisualObj(ushort id, string resource, int spriteWidth, int spriteHeight, Position pos)
            : base(id, pos)
        {
            _tempSpriteSheet = RenderUtils.LoadSpriteSheet(resource, spriteWidth, spriteHeight);
            SpriteWidth = spriteWidth;
            SpriteHeight = spriteHeight;
        }

        public override bool Move(FacingDirection facing, bool run, bool ignoreLegalCheck)
        {
            _leg = !_leg;
            return base.Move(facing, run, ignoreLegalCheck);
        }
        public override void Face(FacingDirection facing)
        {
            _leg = !_leg;
            base.Face(facing);
        }
        public override void CopyMovement(Obj other)
        {
            if (other is VisualObj v)
            {
                _leg = v._leg;
            }
            base.CopyMovement(other);
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
