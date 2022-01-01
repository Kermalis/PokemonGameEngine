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
        public readonly byte CharWidth;
        public readonly byte CharSpace;
        public readonly AtlasPos AtlasPos;

        public Glyph(byte[] dst, Pos2D posInAtlas, Size2D atlasSize, Font parent, PackedGlyph g)
        {
            CharWidth = g.CharWidth;
            CharSpace = g.CharSpace;
            if (CharWidth == 0)
            {
                return;
            }
            AtlasPos = new AtlasPos(new Rect2D(posInAtlas, new Size2D(CharWidth, parent.FontHeight)), atlasSize);

            // Draw to texture atlas
            byte[] packed = g.PackedBitmap;
            byte bpp = parent.BitsPerPixel;

            int curBit = 0;
            int curByte = 0;
            Pos2D pos;
            for (pos.Y = 0; pos.Y < parent.FontHeight; pos.Y++)
            {
                for (pos.X = 0; pos.X < CharWidth; pos.X++)
                {
                    int colorIndex = (packed[curByte] >> (8 - bpp - curBit)) % (1 << bpp);
                    dst[UnsafeRenderer.GetPixelIndex(atlasSize.Width, pos + posInAtlas)] = (byte)colorIndex; // Only set the R component
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
