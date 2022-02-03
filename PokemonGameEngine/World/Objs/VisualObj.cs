using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders.World;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal abstract class VisualObj : Obj
    {
        public static List<VisualObj> LoadedVisualObjs = new();

        /// <summary>Gets updated every frame by the map renderer</summary>
        public Vec2I BlockPosOnScreen;

        protected bool _leg;

        private readonly VisualObjTexture _tex;

        protected VisualObj(ushort id, string imageId)
            : base(id)
        {
            _tex = VisualObjTexture.LoadOrGet(imageId);
            LoadedVisualObjs.Add(this);
        }
        protected VisualObj(ushort id, string imageId, WorldPos pos)
            : base(id, pos)
        {
            _tex = VisualObjTexture.LoadOrGet(imageId);
            LoadedVisualObjs.Add(this);
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

        public void Draw(VisualObjShader shader, Vec2I viewSize)
        {
            // Center align X, bottom align y
            Vec2I pos = _tex.ImageSize - Overworld.Block_NumPixels;
            pos.X /= 2;
            pos = BlockPosOnScreen - pos;

            // Draw obj
            var objRect = Rect.FromSize(pos, _tex.ImageSize);
            if (objRect.Intersects(viewSize))
            {
                bool showMoving = MovementProgress is (not 1f) and (>= 0.6f);
                _tex.RenderImage(shader, objRect, GetImage(showMoving));
            }
        }
        public void DrawShadow(Vec2I viewSize)
        {
            // Left align X, bottom align y (starts in block under)
            Vec2I shadowPos = _tex.ShadowOffset;
            shadowPos += BlockPosOnScreen;
            shadowPos.Y += Overworld.Block_NumPixelsY;

            // Draw shadow
            FrameBuffer.FBOTexture shadow = _tex.Shadow.ColorTextures[0];
            var rect = Rect.FromSize(shadowPos, shadow.Size);
            if (rect.Intersects(viewSize))
            {
                GUIRenderer.Texture(shadow.Texture, rect, new UV(false, false));
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _tex.DeductReference();
            LoadedVisualObjs.Remove(this);
        }
    }
}
