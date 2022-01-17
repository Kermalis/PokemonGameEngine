using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.World;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders.World
{
    internal struct VBOData_InstancedLayoutBlock
    {
        private const int OFFSET_TRANSLATION = 0;
        private const int OFFSET_TEXTURE = OFFSET_TRANSLATION + (2 * sizeof(int));
        private const uint SIZE = OFFSET_TEXTURE + sizeof(float);

        public readonly Vec2I Translation;
        public readonly float Texture;

        private VBOData_InstancedLayoutBlock(float texture, Vec2I translation)
        {
            Translation = translation;
            Texture = texture;
        }

        public static unsafe void AddInstance(InstancedData inst, Vec2I translation, int usedBlockId)
        {
            GL gl = Display.OpenGL;
            var data = new VBOData_InstancedLayoutBlock(usedBlockId / (Blockset.UsedBlocksTextures[0].NumLayers - 1f), translation);
            inst.AddInstance(gl, &data, SIZE);
        }

        public static InstancedData CreateInstancedData(int maxVisible)
        {
            GL gl = Display.OpenGL;
            uint vbo = InstancedData.CreateInstancedVBO(gl, SIZE * (uint)maxVisible, BufferUsageARB.StreamDraw);
            InstancedData.AddInstancedAttribute(gl, 1, 2, SIZE, OFFSET_TRANSLATION);
            InstancedData.AddInstancedAttribute(gl, 2, 1, SIZE, OFFSET_TEXTURE);
            return new InstancedData(vbo, maxVisible);
        }
    }
}
