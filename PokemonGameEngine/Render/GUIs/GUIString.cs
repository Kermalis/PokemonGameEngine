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

        private readonly RectMesh _mesh;
        private readonly InstancedData _data;

        public uint VisibleStart;
        public uint NumVisible;

        public GUIString(string text, Font font, Vector4[] colors, Vec2I pos = default, int scale = 1, bool allVisible = true)
        {
            Text = text;
            Font = font;
            Colors = colors;
            Origin = pos;
            Scale = scale;

            GL gl = Display.OpenGL;
            _mesh = new RectMesh(gl);
            _data = VBOData_InstancedFontChar.CreateInstancedData(font.GetNumVisibleChars(text));

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
                    VBOData_InstancedFontChar.AddInstance(_data, Rect.FromSize(gPos, size), g.UV);
                }
            }
            NumVisible = allVisible ? _data.InstanceCount : 0;
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
        public void Render()
        {
            if (_data.InstanceCount == 0 || NumVisible == 0)
            {
                return;
            }

            GL gl = Display.OpenGL;
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, Font.Texture);

            FontShader shader = FontShader.Instance;
            shader.Use(gl);
            shader.UpdateViewport(gl, Display.ViewportSize);
            shader.SetTranslation(gl, ref Translation);
            shader.SetColors(gl, Colors);

            _mesh.RenderInstancedBaseInstance(gl, VisibleStart, NumVisible);
        }

        public void Delete()
        {
            GL gl = Display.OpenGL;
            _mesh.Delete(gl);
            _data.Delete(gl);
        }
    }
}
