using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders.GUIs;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal sealed class GUIString
    {
        public readonly string Text;
        public readonly Font Font;
        public Vector4[] Colors;
        public readonly Vec2I Origin;
        public Vec2I Translation;
        public readonly int Scale;

        private readonly uint _vao;
        private readonly uint _vbo;
        private readonly uint _ebo;
        private readonly uint _totalVisible;

        public uint VisibleStart;
        public uint NumVisible;

        public GUIString(string text, Font font, Vector4[] colors, Vec2I pos = default, int scale = 1, bool allVisible = true)
        {
            Text = text;
            Font = font;
            Colors = colors;
            Origin = pos;
            Scale = scale;

            // Write glyph vertices
            // May not necessarily use this many, because some glyphs don't have visual results
            var builder = new FontVertexBuilder(text.Length);
            _totalVisible = 0;
            int index = 0;
            var cursor = new Vec2I(0, 0);
            Vec2I size;
            size.Y = font.FontHeight * scale;
            while (index < text.Length)
            {
                Vec2I gPos = (cursor * scale) + pos;
                Glyph g = font.GetGlyph(text, ref index, ref cursor, out _);
                if (g is not null)
                {
                    size.X = g.CharWidth * scale;
                    // Can't use triangle strips, but possibly can use instanced data?
                    builder.Add(Rect.FromSize(gPos, size), g.UV);
                    _totalVisible++;
                }
            }
            NumVisible = allVisible ? _totalVisible : 0;

            builder.Finish(out _vao, out _vbo, out _ebo);
        }

        public static GUIString CreateCentered(string text, Font font, Vector4[] colors, float centerX, float centerY, Vec2I totalSize, int scale = 1)
        {
            Vec2I size = font.GetSize(text);
            return new GUIString(text, font, colors, Vec2I.Center(centerX, centerY, size, totalSize), scale: scale);
        }
        public static void CreateAndRenderOneTimeString(string text, Font font, Vector4[] colors, Vec2I pos, int scale = 1)
        {
            var s = new GUIString(text, font, colors, pos, scale: scale);
            s.Render();
            s.Delete();
        }
        public static void CreateAndRenderOneTimeGenderString(PBEGender gender, Font font, Vec2I pos, int scale = 1)
        {
            if (gender == PBEGender.Genderless)
            {
                return;
            }
            CreateAndRenderOneTimeString(gender.ToSymbol(), font, gender == PBEGender.Male ? FontColors.DefaultBlue_O : FontColors.DefaultRed_O, pos, scale: scale);
        }

        public void Render(Vec2I translation)
        {
            Translation = translation;
            Render();
        }
        public void Render(Vec2I translation, Vector4[] colors)
        {
            Colors = colors;
            Translation = translation;
            Render();
        }
        public unsafe void Render()
        {
            if (_totalVisible == 0 || NumVisible == 0)
            {
                return;
            }

            GL gl = Display.OpenGL;
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, Font.Texture);
            gl.BindVertexArray(_vao);

            FontShader shader = FontShader.Instance;
            shader.Use(gl);
            shader.UpdateViewport(gl, Display.ViewportSize);
            shader.SetTranslation(gl, ref Translation);
            shader.SetColors(gl, Colors);

            gl.DrawElements(PrimitiveType.Triangles, NumVisible * 6, DrawElementsType.UnsignedInt, (void*)(VisibleStart * 6 * sizeof(uint)));

            gl.Disable(EnableCap.Blend);
        }

        public void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteVertexArray(_vao);
            gl.DeleteBuffer(_vbo);
            gl.DeleteBuffer(_ebo);
        }
    }
}
