using Kermalis.PokemonGameEngine.Render.Shaders;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal sealed class TripleColorBackground
    {
        private readonly TripleColorBackgroundShader _shader;

        public unsafe TripleColorBackground()
        {
            _shader = new TripleColorBackgroundShader(Display.OpenGL);
        }

        public void SetColors(in Vector3 color1, in Vector3 color2, in Vector3 color3)
        {
            GL gl = Display.OpenGL;
            _shader.Use(gl);
            _shader.SetColors(gl, color1, color2, color3);
        }
        public void Render()
        {
            GL gl = Display.OpenGL;
            _shader.Use(gl);
            TripleColorBackgroundMesh.Instance.Render(gl);
        }

        public void Delete()
        {
            _shader.Delete();
        }
    }
}
