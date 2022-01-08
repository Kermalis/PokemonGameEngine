using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.World;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders.World
{
    internal struct VBOData_InstancedLayoutBlock
    {
        private const int OffsetOfTranslation = 0;
        private const int OffsetOfTexture = OffsetOfTranslation + (2 * sizeof(int));
        private const uint SizeOf = OffsetOfTexture + sizeof(float);

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
            inst.AddInstance(gl, &data, SizeOf);
        }

        public static InstancedData CreateInstancedData(int maxVisible)
        {
            GL gl = Display.OpenGL;
            uint vbo = InstancedData.CreateInstancedVBO(gl, SizeOf * (uint)maxVisible);
            InstancedData.AddInstancedAttribute(gl, 1, 2, SizeOf, OffsetOfTranslation);
            InstancedData.AddInstancedAttribute(gl, 2, 1, SizeOf, OffsetOfTexture);
            return new InstancedData(vbo, maxVisible);
        }
    }
}
