using Kermalis.EndianBinaryIO;

namespace Kermalis.PokemonGameEngine.Render.Fonts
{
    internal sealed class PackedGlyph
    {
        public readonly byte CharWidth;
        public readonly byte CharSpace;
        public readonly byte[] PackedBitmap;

        public PackedGlyph(EndianBinaryReader r, Font parent)
        {
            CharWidth = r.ReadByte();
            CharSpace = r.ReadByte();
            int numBitsToRead = parent.FontHeight * CharWidth * parent.BitsPerPixel;
            PackedBitmap = r.ReadBytes((numBitsToRead / 8) + ((numBitsToRead % 8) != 0 ? 1 : 0));
        }
    }
    internal sealed class Glyph
    {
        public readonly Font Parent;
        public readonly byte CharWidth;
        public readonly byte CharSpace;
        public readonly float AtlasStartX;
        public readonly float AtlasEndX;
        public readonly float AtlasStartY;
        public readonly float AtlasEndY;

        public Glyph(byte[] dst, int startX, int startY, uint atlasWidth, uint atlasHeight, Font parent, PackedGlyph g)
        {
            Parent = parent;
            CharWidth = g.CharWidth;
            CharSpace = g.CharSpace;
            if (CharWidth == 0)
            {
                return;
            }
            AtlasStartX = Renderer.AbsXToRelX(startX, atlasWidth);
            AtlasEndX = Renderer.AbsXToRelX(startX + CharWidth, atlasWidth);
            AtlasStartY = Renderer.AbsYToRelY(startY, atlasHeight);
            AtlasEndY = Renderer.AbsYToRelY(startY + parent.FontHeight, atlasHeight);

            // Draw to texture atlas
            byte[] packed = g.PackedBitmap;
            byte bpp = parent.BitsPerPixel;

            int curBit = 0;
            int curByte = 0;
            for (int py = 0; py < parent.FontHeight; py++)
            {
                for (int px = 0; px < CharWidth; px++)
                {
                    int colorIndex = (packed[curByte] >> (8 - bpp - curBit)) % (1 << bpp);
                    dst[Renderer.GetPixelIndex(atlasWidth, px + startX, py + startY)] = (byte)colorIndex; // Only set the R component
                    curBit = (curBit + bpp) % 8;
                    if (curBit == 0)
                    {
                        curByte++;
                    }
                }
            }
        }
    }
}
