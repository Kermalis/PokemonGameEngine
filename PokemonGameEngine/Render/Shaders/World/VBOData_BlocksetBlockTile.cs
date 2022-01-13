using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.World
{
    internal struct VBOData_BlocksetBlockTile
    {
        private const int OFFSET_POS = 0;
        private const int OFFSET_TILESET = OFFSET_POS + (2 * sizeof(int));
        private const int OFFSET_UV = OFFSET_TILESET + sizeof(int);
        public const uint SIZE = OFFSET_UV + (2 * sizeof(float));

        public readonly Vec2I Pos;
        public readonly int Tileset;
        public readonly Vector2 UV;

        public VBOData_BlocksetBlockTile(Vec2I pos, int tileset, Vector2 uv)
        {
            Pos = pos;
            Tileset = tileset;
            UV = uv;
        }

        public static unsafe void AddAttributes(GL gl)
        {
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, SIZE, (void*)OFFSET_POS);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, SIZE, (void*)OFFSET_TILESET);
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, SIZE, (void*)OFFSET_UV);
        }
    }
}
