using Kermalis.PokemonGameEngine.Render;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract class VisualObj : Obj
    {
        private bool _leg;

        private readonly ImageSheet _sheet;

        protected VisualObj(ushort id, string imageId)
            : base(id)
        {
            _sheet = ImageSheet.LoadOrGet(imageId);
        }
        protected VisualObj(ushort id, string imageId, Position pos)
            : base(id, pos)
        {
            _sheet = ImageSheet.LoadOrGet(imageId);
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

        protected virtual int GetImage(bool showMoving)
        {
            byte f = (byte)Facing;
            if (!showMoving)
            {
                return f;
            }
            return _leg ? f + 8 : f + 16; // TODO: Fall-back to specific images if the target image doesn't exist
        }

        // TODO: Water reflections, priority
        public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int blockX, int blockY, int startBlockPixelX, int startBlockPixelY)
        {
            int baseX = (blockX * Overworld.Block_NumPixelsX) + ProgressX + startBlockPixelX;
            int baseY = (blockY * Overworld.Block_NumPixelsY) + ProgressY + startBlockPixelY;
            // Calc img coords
            ImageSheet s = _sheet;
            int w = s.ImageWidth;
            int h = s.ImageHeight;
            int x = baseX - ((w - Overworld.Block_NumPixelsX) / 2); // Center align
            int y = baseY - (h - Overworld.Block_NumPixelsY); // Bottom align
            // Calc shadow coords
            Image shadow = s.ShadowImage;
            int sw = shadow.Width;
            int sh = shadow.Height;
            int sx = baseX + s.ShadowXOffset; // Left align
            int sy = baseY + Overworld.Block_NumPixelsY + s.ShadowYOffset; // Bottom align (starts in block under)

            // Draw shadow image
            if (RenderUtils.IsInsideBitmap(bmpWidth, bmpHeight, sx, sy, sw, sh))
            {
                shadow.DrawOn(bmpAddress, bmpWidth, bmpHeight, sx, sy);
            }
            // Draw obj image
            if (RenderUtils.IsInsideBitmap(bmpWidth, bmpHeight, x, y, w, h))
            {
                float t = MovementTimer;
                bool showMoving = t != 1 && t >= 0.6f;
                int imgNum = GetImage(showMoving);
                s.Images[imgNum].DrawOn(bmpAddress, bmpWidth, bmpHeight, x, y);
            }
        }
    }
}
