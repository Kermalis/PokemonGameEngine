using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.GUIs
{
    internal struct VBOData_TripleColorBackground
    {
        private const int OFFSET_POS = 0;
        private const int OFFSET_COLOR = OFFSET_POS + (2 * sizeof(float));
        public const uint SIZE = OFFSET_COLOR + sizeof(int);

        public readonly Vector2 Pos;
        public readonly int Color;

        public VBOData_TripleColorBackground(Vector2 pos, int color)
        {
            Pos = pos;
            Color = color;
        }

        public static unsafe void AddAttributes(GL gl)
        {
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, SIZE, (void*)OFFSET_POS);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, SIZE, (void*)OFFSET_COLOR);
        }
    }
}
