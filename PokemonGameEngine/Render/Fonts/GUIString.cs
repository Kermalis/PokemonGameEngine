using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Fonts
{
    internal sealed class GUIString
    {
        private struct TexStruct
        {
            public const int OffsetOfPos = 0;
            public const int OffsetOfTexCoords = 2 * sizeof(int);
            public const uint SizeOf = (2 * sizeof(int)) + (2 * sizeof(float));

            public Pos2D Pos;
            public Vector2 TexCoords;

            public TexStruct(int x, int y, float texX, float texY)
            {
                Pos = new Pos2D(x, y);
                TexCoords = new Vector2(texX, texY);
            }
        }

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
        public readonly int Scale;

        private readonly uint _vao;
        private readonly uint _vbo;
        private readonly uint _ebo;
        private readonly uint _totalVisible;
        private readonly uint _indexCount;

        public uint VisibleStart;
        public uint NumVisible;

        public unsafe GUIString(string text, Font font, ColorF[] colors, Pos2D pos = default, int scale = 1, bool allVisible = true)
        {
            Text = text;
            Font = font;
            Colors = colors;
            Origin = pos;
            Scale = scale;

            // Write glyph vertices
            // May not necessarily use this many, because some glyphs don't have visual results
            var vertices = new TexStruct[text.Length * 4];
            uint[] indices = new uint[text.Length * 6];
            _totalVisible = 0;
            uint vertexCount = 0;
            _indexCount = 0;
            uint nextXOffset = 0;
            uint nextYOffset = 0;
            int index = 0;
            while (index < text.Length)
            {
                int curX = pos.X + (int)(nextXOffset * scale);
                int curY = pos.Y + (int)(nextYOffset * scale);
                Glyph g = font.GetGlyph(text, ref index, ref nextXOffset, ref nextYOffset, out _);
                if (g is null)
                {
                    continue;
                }
                int w = g.CharWidth * scale;
                int h = font.FontHeight * scale;
                float texX = g.AtlasStartX;
                float texEndX = g.AtlasEndX;
                float texY = g.AtlasStartY;
                float texEndY = g.AtlasEndY;
                // Can't use triangle strips
                uint vIndex = vertexCount;
                vertexCount += 4;
                vertices[vIndex + 0] = new TexStruct(curX, curY, texX, texY);
                vertices[vIndex + 1] = new TexStruct(curX, curY + h, texX, texEndY);
                vertices[vIndex + 2] = new TexStruct(curX + w, curY, texEndX, texY);
                vertices[vIndex + 3] = new TexStruct(curX + w, curY + h, texEndX, texEndY);
                indices[_indexCount++] = vIndex + 0;
                indices[_indexCount++] = vIndex + 1;
                indices[_indexCount++] = vIndex + 2;
                indices[_indexCount++] = vIndex + 2;
                indices[_indexCount++] = vIndex + 1;
                indices[_indexCount++] = vIndex + 3;
                _totalVisible++;
            }
            NumVisible = allVisible ? _totalVisible : 0;

            // Create vao
            GL gl = Game.OpenGL;
            _vao = gl.GenVertexArray();
            gl.BindVertexArray(_vao);

            // Store in vbo
            _vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (void* d = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, TexStruct.SizeOf * vertexCount, d, BufferUsageARB.StaticDraw);
            }
            // Store in ebo
            _ebo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            fixed (void* d = indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, sizeof(uint) * _indexCount, d, BufferUsageARB.StaticDraw);
            }

            // Now set attribs
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, TexStruct.SizeOf, (void*)TexStruct.OffsetOfPos);
            gl.DisableVertexAttribArray(0);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, TexStruct.SizeOf, (void*)TexStruct.OffsetOfTexCoords);
            gl.DisableVertexAttribArray(1);

            gl.BindVertexArray(0);
        }

        public static GUIString CreateCentered(string text, Font font, ColorF[] colors, float centerX, float centerY, int scale = 1)
        {
            Size2D s = font.MeasureString(text);
            return new GUIString(text, font, colors, Pos2D.Center(centerX, centerY, s), scale: scale);
        }
        public static void CreateAndRenderOneTimeString(GL gl, string text, Font font, ColorF[] colors, Pos2D pos, int scale = 1)
        {
            var s = new GUIString(text, font, colors, pos, scale: scale);
            s.Render(gl);
            s.Delete(gl);
        }
        public static void CreateAndRenderOneTimeGenderString(GL gl, PBEGender gender, Font font, Pos2D pos, int scale = 1)
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

            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
            GLHelper.EnableBlend(gl, true);
            GLHelper.BlendFunc(gl, BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
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
