using Kermalis.PokemonGameEngine.Render;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract class VisualObj : Obj
    {
        private bool _leg;

        private readonly SpriteSheet _sprite;

        protected VisualObj(ushort id, string sprite)
            : base(id)
        {
            _sprite = SpriteSheet.LoadOrGet(sprite);
        }
        protected VisualObj(ushort id, string sprite, Position pos)
            : base(id, pos)
        {
            _sprite = SpriteSheet.LoadOrGet(sprite);
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

        // TODO: Water reflections, priority
        public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int startBlockX, int startBlockY, int startPixelX, int startPixelY)
        {
            Position pos = Pos;
            int baseX = ((pos.X - startBlockX) * Overworld.Block_NumPixelsX) + ProgressX + startPixelX;
            int baseY = ((pos.Y - startBlockY) * Overworld.Block_NumPixelsY) + ProgressY + startPixelY;
            // Calc sprite coords
            SpriteSheet ss = _sprite;
            int w = ss.SpriteWidth;
            int h = ss.SpriteHeight;
            int x = baseX - ((w - Overworld.Block_NumPixelsX) / 2); // Center align
            int y = baseY - (h - Overworld.Block_NumPixelsY); // Bottom align
            // Calc shadow coords
            Sprite shadow = ss.ShadowSprite;
            int sw = shadow.Width;
            int sh = shadow.Height;
            int sx = baseX + ss.ShadowXOffset; // Left align
            int sy = baseY + Overworld.Block_NumPixelsY + ss.ShadowYOffset; // Bottom align (starts in block under)

            if (!RenderUtils.IsInsideBitmap(bmpWidth, bmpHeight, x, y, w, h)
                && !RenderUtils.IsInsideBitmap(bmpWidth, bmpHeight, sx, sy, sw, sh))
            {
                return; // Return if no pixel is inside of the bitmap
            }

            // Draw shadow sprite
            shadow.DrawOn(bmpAddress, bmpWidth, bmpHeight, sx, sy);
            // Draw obj sprite
            bool ShowLegs()
            {
                float t = MovementTimer;
                return t != 1 && t >= 0.6f;
            }
            byte f = (byte)Facing;
            int spriteNum = ShowLegs() ? (_leg ? f + 8 : f + 16) : f; // TODO: Fall-back to specific sprites if the target sprite doesn't exist
            ss.Sprites[spriteNum].DrawOn(bmpAddress, bmpWidth, bmpHeight, x, y);
        }
    }
}
