using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.Battle
{
    internal struct VBOData_BattleModel
    {
        private const int OFFSET_POS = 0;
        private const int OFFSET_NORMAL = OFFSET_POS + (3 * sizeof(float));
        private const int OFFSET_UV = OFFSET_NORMAL + (3 * sizeof(float));
        public const uint SIZE = OFFSET_UV + (2 * sizeof(float));

        public Vector3 Pos;
        public Vector3 Normal;
        public Vector2 UV;

        public static unsafe void AddAttributes(GL gl)
        {
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, SIZE, (void*)OFFSET_POS);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, SIZE, (void*)OFFSET_NORMAL);
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, SIZE, (void*)OFFSET_UV);
        }
    }
}
