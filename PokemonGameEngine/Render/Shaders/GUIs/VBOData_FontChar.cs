using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.GUIs
{
    internal struct VBOData_FontChar
    {
        public const int OffsetOfPos = 0;
        public const int OffsetOfUV = OffsetOfPos + (2 * sizeof(int));
        public const uint SizeOf = OffsetOfUV + (2 * sizeof(float));

        public readonly Vec2I Pos;
        public readonly Vector2 UV;

        public VBOData_FontChar(Vec2I pos, Vector2 uv)
        {
            Pos = pos;
            UV = uv;
        }
    }
}
