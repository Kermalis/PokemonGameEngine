using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Images;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract class VisualObj : Obj
    {
        protected bool _leg;

        private readonly ImageSheet _sheet;

        protected VisualObj(ushort id, string imageId)
            : base(id)
        {
            _sheet = ImageSheet.LoadOrGet(imageId);
        }
        protected VisualObj(ushort id, string imageId, WorldPos pos)
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

        protected virtual uint GetImage(bool showMoving)
        {
            byte f = (byte)Facing;
            if (!showMoving)
            {
                return f;
            }
            return _leg ? f + 8u : f + 16u; // TODO: Fall-back to specific images if the target image doesn't exist
        }

        // TODO: Water reflections
        public void Draw(Pos2D blockPos)
        {
            // Calc img coords
            ImageSheet s = _sheet;
            Size2D size = s.ImageSize;
            Pos2D pos;
            pos.X = blockPos.X - (((int)size.Width - Overworld.Block_NumPixelsX) / 2); // Center align
            pos.Y = blockPos.Y - ((int)size.Height - Overworld.Block_NumPixelsY); // Bottom align
            // Calc shadow coords
            WriteableImage shadow = s.ShadowImage;
            Pos2D shadowPos = s.ShadowOffset;
            shadowPos.X += blockPos.X; // Left align
            shadowPos.Y += blockPos.Y + Overworld.Block_NumPixelsY; // Bottom align (starts in block under)

            // Draw shadow image
            if (new Rect2D(shadowPos, shadow.Size).Intersects(Game.RenderSize))
            {
                shadow.Render(shadowPos);
            }
            // Draw obj image
            var objRect = new Rect2D(pos, size);
            if (objRect.Intersects(Game.RenderSize))
            {
                float t = MovementTimer;
                bool showMoving = t != 1 && t >= 0.6f;
                uint imgNum = GetImage(showMoving);
                s.Images.Render(objRect, s.GetAtlasPos(imgNum));
            }
        }

        public override void Dispose()
        {
            GL gl = Game.OpenGL;
            _sheet.DeductReference(gl);
        }
    }
}
