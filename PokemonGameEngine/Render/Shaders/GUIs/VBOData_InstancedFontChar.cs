using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.GUIs
{
    internal struct VBOData_InstancedFontChar
    {
        private const int OffsetOfPos = 0;
        private const int OffsetOfSize = OffsetOfPos + (2 * sizeof(int));
        private const int OffsetOfUVStart = OffsetOfSize + (2 * sizeof(int));
        private const int OffsetOfUVEnd = OffsetOfUVStart + (2 * sizeof(float));
        private const uint SizeOf = OffsetOfUVEnd + (2 * sizeof(float));

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
            inst.AddInstance(gl, &data, SizeOf);
        }

        public static InstancedData CreateInstancedData(int maxVisible)
        {
            GL gl = Display.OpenGL;
            uint vbo = InstancedData.CreateInstancedVBO(gl, SizeOf * (uint)maxVisible, BufferUsageARB.StaticDraw);
            InstancedData.AddInstancedAttribute(gl, 1, 2, SizeOf, OffsetOfPos);
            InstancedData.AddInstancedAttribute(gl, 2, 2, SizeOf, OffsetOfSize);
            InstancedData.AddInstancedAttribute(gl, 3, 2, SizeOf, OffsetOfUVStart);
            InstancedData.AddInstancedAttribute(gl, 4, 2, SizeOf, OffsetOfUVEnd);
            return new InstancedData(vbo, maxVisible);
        }
    }
}
