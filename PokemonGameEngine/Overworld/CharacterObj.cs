using Kermalis.PokemonGameEngine.Util;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class CharacterObj : Obj
    {
        public readonly int SpriteWidth;
        public readonly int SpriteHeight;
        private readonly uint[][][] _tempSpriteSheet;

        public byte Z;

        public CharacterObj(ushort id, string resource, int spriteWidth, int spriteHeight) : base(id)
        {
            SpriteWidth = spriteWidth;
            SpriteHeight = spriteHeight;
            _tempSpriteSheet = RenderUtil.LoadSpriteSheet(resource, SpriteWidth, SpriteHeight);
        }

        public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
        {
            RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, x, y, _tempSpriteSheet[0], false, false);
        }
    }
}
