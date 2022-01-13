using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.GUIs
{
    internal struct VBOData_InstancedFontChar
    {
        private const int OFFSET_POS = 0;
        private const int OFFSET_SIZE = OFFSET_POS + (2 * sizeof(int));
        private const int OFFSET_UVSTART = OFFSET_SIZE + (2 * sizeof(int));
        private const int OFFSET_UVEND = OFFSET_UVSTART + (2 * sizeof(float));
        private const uint SIZE = OFFSET_UVEND + (2 * sizeof(float));

        public readonly Vec2I Pos;
        public readonly Vec2I Size;
        public readonly Vector2 UVStart;
        public readonly Vector2 UVEnd;

        private VBOData_InstancedFontChar(in Rect rect, in UV uv)
        {
            Pos = rect.TopLeft;
            Size = rect.GetSize();
            UVStart = uv.Start;
            UVEnd = uv.End;
        }

        public static unsafe void AddInstance(InstancedData inst, in Rect rect, in UV uv)
        {
            GL gl = Display.OpenGL;
            var data = new VBOData_InstancedFontChar(rect, uv);
            inst.AddInstance(gl, &data, SIZE);
        }

        public static InstancedData CreateInstancedData(int maxVisible)
        {
            GL gl = Display.OpenGL;
            uint vbo = InstancedData.CreateInstancedVBO(gl, SIZE * (uint)maxVisible, BufferUsageARB.StaticDraw);
            InstancedData.AddInstancedAttribute(gl, 1, 2, SIZE, OFFSET_POS);
            InstancedData.AddInstancedAttribute(gl, 2, 2, SIZE, OFFSET_SIZE);
            InstancedData.AddInstancedAttribute(gl, 3, 2, SIZE, OFFSET_UVSTART);
            InstancedData.AddInstancedAttribute(gl, 4, 2, SIZE, OFFSET_UVEND);
            return new InstancedData(vbo, maxVisible);
        }
    }
}
