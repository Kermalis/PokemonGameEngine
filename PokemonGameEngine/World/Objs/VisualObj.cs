namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract class VisualObj : Obj
    {
        private bool _leg;

        public readonly SpriteSheet Sprite;

        protected VisualObj(ushort id, string sprite)
            : base(id)
        {
            Sprite = SpriteSheet.LoadOrGet(sprite);
        }
        protected VisualObj(ushort id, string sprite, Position pos)
            : base(id, pos)
        {
            Sprite = SpriteSheet.LoadOrGet(sprite);
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
        public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int startBlockX, int startBlockY, int startPixelX, int startPixelY)
        {
            Position pos = Pos;
            int x = ((pos.X - startBlockX) * Overworld.Block_NumPixelsX) + _progressX + startPixelX;
            int y = ((pos.Y - startBlockY) * Overworld.Block_NumPixelsY) + _progressY + startPixelY;
            int w = Sprite.SpriteWidth;
            int h = Sprite.SpriteHeight;
            x -= (w - Overworld.Block_NumPixelsX) / 2; // Center align
            y -= h - Overworld.Block_NumPixelsY; // Bottom align
            if (x >= bmpWidth || x + w <= 0 || y >= bmpHeight || y + h <= 0)
            {
                return; // Return if no pixel is inside of the bitmap
            }
            bool ShowLegs()
            {
                float t = _movementTimer;
                return t != 1 && t >= 0.6f;
            }
            byte f = (byte)Facing;
            int spriteNum = ShowLegs() ? (_leg ? f + 8 : f + 16) : f; // TODO: Fall-back to specific sprites if the target sprite doesn't exist
            Sprite.Sprites[spriteNum].DrawOn(bmpAddress, bmpWidth, bmpHeight, x, y);
        }
    }
}
