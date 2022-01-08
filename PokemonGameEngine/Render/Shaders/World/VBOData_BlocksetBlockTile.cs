using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.World
{
    internal struct VBOData_BlocksetBlockTile
    {
        public const int OffsetOfPos = 0;
        public const int OffsetOfTileset = OffsetOfPos + (2 * sizeof(int));
        public const int OffsetOfUV = OffsetOfTileset + sizeof(int);
        public const uint SizeOf = OffsetOfUV + (2 * sizeof(float));

        public readonly Vec2I Pos;
        public readonly int Tileset;
        public readonly Vector2 UV;

        public VBOData_BlocksetBlockTile(Vec2I pos, int tileset, Vector2 uv)
        {
            Pos = pos;
            Tileset = tileset;
            UV = uv;
        }
    }
}
