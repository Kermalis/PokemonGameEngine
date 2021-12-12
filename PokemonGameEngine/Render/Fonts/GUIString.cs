using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Fonts
{
    internal sealed class GUIString
    {
        public readonly string Text;
        public readonly Font Font;
        public Vector4[] Colors;
        public readonly Pos2D Origin;
        public Pos2D Translation;
        public readonly uint Scale;

        private readonly uint _vao;
        private readonly uint _vbo;
        private readonly uint _ebo;
        private readonly uint _totalVisible;

        public uint VisibleStart;
        public uint NumVisible;

        public GUIString(string text, Font font, Vector4[] colors, Pos2D pos = default, uint scale = 1, bool allVisible = true)
        {
            Text = text;
            Font = font;
            Colors = colors;
            Origin = pos;
            Scale = scale;

            // Write glyph vertices
            // May not necessarily use this many, because some glyphs don't have visual results
            var builder = new TexVertexBuilder(text.Length);
            _totalVisible = 0;
            uint nextXOffset = 0;
            uint nextYOffset = 0;
            int index = 0;
            Size2D size;
            size.Height = font.FontHeight * scale;
            while (index < text.Length)
            {
                Pos2D curPos;
                curPos.X = pos.X + (int)(nextXOffset * scale);
                curPos.Y = pos.Y + (int)(nextYOffset * scale);
                Glyph g = font.GetGlyph(text, ref index, ref nextXOffset, ref nextYOffset, out _);
                if (g is null)
                {
                    continue;
                }
                size.Width = g.CharWidth * scale;
                // Can't use triangle strips
                builder.Add(new Rect2D(curPos, size), g.AtlasPos);
                _totalVisible++;
            }
            NumVisible = allVisible ? _totalVisible : 0;

            builder.Finish(out _, out _vao, out _vbo, out _ebo);
        }

        public static GUIString CreateCentered(string text, Font font, Vector4[] colors, float centerX, float centerY, uint scale = 1)
        {
            Size2D s = font.MeasureString(text);
            return new GUIString(text, font, colors, Pos2D.Center(centerX, centerY, s), scale: scale);
        }
        public static void CreateAndRenderOneTimeString(string text, Font font, Vector4[] colors, Pos2D pos, uint scale = 1)
        {
            var s = new GUIString(text, font, colors, pos, scale: scale);
            s.Render();
            s.Delete();
        }
        public static void CreateAndRenderOneTimeGenderString(PBEGender gender, Font font, Pos2D pos, uint scale = 1)
        {
            if (gender == PBEGender.Genderless)
            {
                return;
            }
            CreateAndRenderOneTimeString(gender.ToSymbol(), font, gender == PBEGender.Male ? FontColors.DefaultBlue_O : FontColors.DefaultRed_O, pos, scale: scale);
        }

        public void Render(Pos2D translation)
        {
            Translation = translation;
            Render();
        }
        public void Render(Pos2D translation, Vector4[] colors)
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
            shader.SetResolution(gl);
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
