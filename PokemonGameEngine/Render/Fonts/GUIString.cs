using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Data.Utils;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Fonts
{
    internal sealed class GUIString
    {
        private static readonly FontShader _shader;

        static GUIString()
        {
            GL gl = Game.OpenGL;
            _shader = new FontShader(gl);
        }

        public readonly string Text;
        public readonly Font Font;
        public ColorF[] Colors;
        public readonly Pos2D Origin;
        public Pos2D Translation;
        public readonly uint Scale;

        private readonly uint _vao;
        private readonly uint _vbo;
        private readonly uint _ebo;
        private readonly uint _totalVisible;

        public uint VisibleStart;
        public uint NumVisible;

        public GUIString(string text, Font font, ColorF[] colors, Pos2D pos = default, uint scale = 1, bool allVisible = true)
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

            builder.Finish(Game.OpenGL, out _, out _vao, out _vbo, out _ebo);
        }

        public static GUIString CreateCentered(string text, Font font, ColorF[] colors, float centerX, float centerY, uint scale = 1)
        {
            Size2D s = font.MeasureString(text);
            return new GUIString(text, font, colors, Pos2D.Center(centerX, centerY, s), scale: scale);
        }
        public static void CreateAndRenderOneTimeString(GL gl, string text, Font font, ColorF[] colors, Pos2D pos, uint scale = 1)
        {
            var s = new GUIString(text, font, colors, pos, scale: scale);
            s.Render(gl);
            s.Delete(gl);
        }
        public static void CreateAndRenderOneTimeGenderString(GL gl, PBEGender gender, Font font, Pos2D pos, uint scale = 1)
        {
            if (gender == PBEGender.Genderless)
            {
                return;
            }
            CreateAndRenderOneTimeString(gl, gender.ToSymbol(), font, gender == PBEGender.Male ? FontColors.DefaultBlue_O : FontColors.DefaultRed_O, pos, scale: scale);
        }

        public void Render(GL gl, Pos2D translation)
        {
            Translation = translation;
            Render(gl);
        }
        public void Render(GL gl, Pos2D translation, ColorF[] colors)
        {
            Colors = colors;
            Translation = translation;
            Render(gl);
        }
        public unsafe void Render(GL gl)
        {
            if (_totalVisible == 0 || NumVisible == 0)
            {
                return;
            }

            GLHelper.EnableBlend(gl, true);
            GLHelper.BlendFunc(gl, BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
            GLHelper.BindTexture(gl, Font.Texture);
            gl.BindVertexArray(_vao);
            gl.EnableVertexAttribArray(0);
            gl.EnableVertexAttribArray(1);
            _shader.Use(gl);
            _shader.SetResolution(gl);
            _shader.SetTranslation(gl, ref Translation);
            _shader.SetTextureUnit(gl, 0);
            _shader.SetColors(gl, Colors);

            gl.DrawElements(PrimitiveType.Triangles, NumVisible * 6, DrawElementsType.UnsignedInt, (void*)(VisibleStart * 6 * sizeof(uint)));

            GLHelper.EnableBlend(gl, false);
            GLHelper.BindTexture(gl, 0);
            gl.DisableVertexAttribArray(0);
            gl.DisableVertexAttribArray(1);
            gl.BindVertexArray(0);
            gl.UseProgram(0);
        }

        public void Delete(GL gl)
        {
            gl.DeleteVertexArray(_vao);
            gl.DeleteBuffer(_vbo);
            gl.DeleteBuffer(_ebo);
        }
        public static void GameExit(GL gl)
        {
            _shader.Delete(gl);
        }
    }
}
