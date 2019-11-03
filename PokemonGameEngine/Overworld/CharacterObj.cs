using Kermalis.PokemonGameEngine.Util;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class CharacterObj : Obj
    {
        public static readonly CharacterObj Player = new CharacterObj(PlayerId, "TestNPC.png", 32, 32);

        public readonly int SpriteWidth;
        public readonly int SpriteHeight;
        private readonly uint[][][] _tempSpriteSheet;

        public byte Z;

        public CharacterObj(ushort id, string resource, int spriteWidth, int spriteHeight)
            : base(id)
        {
            SpriteWidth = spriteWidth;
            SpriteHeight = spriteHeight;
            _tempSpriteSheet = RenderUtil.LoadSpriteSheet(resource, SpriteWidth, SpriteHeight);
        }

        public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
        {
            bool ShowLegs()
            {
                return MovementTimer != 0 && MovementTimer <= 0.6f;
            }
            byte f = (byte)Facing;
            int spriteNum = ShowLegs() ? (_leg ? f + 8 : f + 16) : f; // TODO: Fall-back to specific sprites if the target sprite doesn't exist
            RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, x, y, _tempSpriteSheet[spriteNum], false, false);
        }
    }
}
