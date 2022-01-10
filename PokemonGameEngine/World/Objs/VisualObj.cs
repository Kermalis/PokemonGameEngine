using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract class VisualObj : Obj
    {
        protected bool _leg;

        private readonly VisualObjTexture _tex;

        protected VisualObj(ushort id, string imageId)
            : base(id)
        {
            _tex = VisualObjTexture.LoadOrGet(imageId);
        }
        protected VisualObj(ushort id, string imageId, WorldPos pos)
            : base(id, pos)
        {
            _tex = VisualObjTexture.LoadOrGet(imageId);
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

        // TODO: Water reflections
        public void Draw(Vec2I viewSize, Vec2I blockPos)
        {
            // Image pos
            // Center align X, bottom align y
            Vec2I pos = _tex.ImageSize - Overworld.Block_NumPixels;
            pos.X /= 2;
            pos = blockPos - pos;

            // Shadow pos
            Vec2I shadowPos = _tex.ShadowOffset;
            shadowPos += blockPos;
            // Left align X, bottom align y (starts in block under)
            shadowPos.Y += Overworld.Block_NumPixelsY;

            // Draw shadow
            var rect = Rect.FromSize(shadowPos, _tex.Shadow.Size);
            if (rect.Intersects(viewSize))
            {
                GUIRenderer.Texture(_tex.Shadow.ColorTexture, rect, new UV(false, false));
            }

            // Draw obj
            var objRect = Rect.FromSize(pos, _tex.ImageSize);
            if (objRect.Intersects(viewSize))
            {
                bool showMoving = MovementProgress is (not 1f) and (>= 0.6f);
                _tex.RenderImage(objRect, GetImage(showMoving));
            }
        }

        public override void Dispose()
        {
            _tex.DeductReference();
        }
    }
}
