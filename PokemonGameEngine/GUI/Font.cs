using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class Font
    {
        public sealed class Glyph
        {
            public Font Parent { get; }

            public byte CharWidth { get; }
            public byte CharSpace { get; }
            public byte[] Bitmap { get; }

            public Glyph(EndianBinaryReader r, Font parent)
            {
                Parent = parent;

                CharWidth = r.ReadByte();
                CharSpace = r.ReadByte();
                int numBitsToRead = parent.FontHeight * CharWidth * parent.BitsPerPixel;
                Bitmap = r.ReadBytes((numBitsToRead / 8) + ((numBitsToRead % 8) != 0 ? 1 : 0));
            }
        }
        public byte FontHeight { get; }
        public byte BitsPerPixel { get; }
        private readonly Dictionary<ushort, Glyph> _glyphs;
        private readonly (string OldKey, ushort NewKey)[] _overrides;

        public static Font Default { get; }
        public static Font DefaultSmall { get; }
        public static uint[] DefaultWhite { get; } = new uint[] { RenderUtils.Color(0, 0, 0, 0), RenderUtils.Color(239, 239, 239, 255), RenderUtils.Color(132, 132, 132, 255) };
        public static uint[] DefaultSelected { get; } = new uint[] { RenderUtils.Color(0, 0, 0, 0), RenderUtils.Color(255, 224, 22, 255), RenderUtils.Color(188, 165, 16, 255) };
        public static uint[] DefaultDisabled { get; } = new uint[] { RenderUtils.Color(0, 0, 0, 0), RenderUtils.Color(133, 133, 141, 255), RenderUtils.Color(58, 50, 50, 255) };
        public static uint[] DefaultDark { get; } = new uint[] { RenderUtils.Color(0, 0, 0, 0), RenderUtils.Color(90, 82, 82, 255), RenderUtils.Color(165, 165, 173, 255) };
        public static uint[] DefaultMale { get; } = new uint[] { RenderUtils.Color(0, 0, 0, 0), RenderUtils.Color(115, 148, 255, 255), RenderUtils.Color(0, 0, 214, 255) };
        public static uint[] DefaultFemale { get; } = new uint[] { RenderUtils.Color(0, 0, 0, 0), RenderUtils.Color(255, 115, 115, 255), RenderUtils.Color(198, 0, 0, 255) };
        public static Font PartyNumbers { get; }

        static Font()
        {
            Default = new Font("Fonts.Default.kermfont", new (string, ushort)[]
            {
                ("♂", 0x246D),
                ("♀", 0x246E),
                ("[PK]", 0x2486),
                ("[MN]", 0x2487)
            });
            DefaultSmall = new Font("Fonts.DefaultSmall.kermfont", Default._overrides);
            PartyNumbers = new Font("Fonts.PartyNumbers.kermfont", new (string, ushort)[]
            {
                ("[ID]", 0x0049),
                ("[LV]", 0x004C),
                ("[NO]", 0x004E)
            });
        }

        private Font(string resource, (string, ushort)[] overrides)
        {
            using (var r = new EndianBinaryReader(Utils.GetResourceStream(resource), Endianness.LittleEndian))
            {
                FontHeight = r.ReadByte();
                BitsPerPixel = r.ReadByte();
                int numGlyphs = r.ReadInt32();
                _glyphs = new Dictionary<ushort, Glyph>(numGlyphs);
                for (int i = 0; i < numGlyphs; i++)
                {
                    _glyphs.Add(r.ReadUInt16(), new Glyph(r, this));
                }
                _overrides = overrides;
            }
        }

        public Glyph GetGlyph(string str, ref int index, ref int xOffset, ref int yOffset, out string readStr)
        {
            char c = str[index];
            if (c == '\r') // Completely ignore CR
            {
                index++;
                readStr = null;
                return null;
            }
            if (c == '\n' || c == '\v')
            {
                index++;
                xOffset = 0;
                yOffset += FontHeight + 1;
                readStr = c.ToString();
                return null;
            }
            if (c == '\f')
            {
                index++;
                xOffset = 0;
                yOffset = 0;
                readStr = "\f";
                return null;
            }
            Glyph ret;
            for (int i = 0; i < _overrides.Length; i++)
            {
                (string oldKey, ushort newKey) = _overrides[i];
                int ol = oldKey.Length;
                if (index + ol <= str.Length && str.Substring(index, ol) == oldKey)
                {
                    index += ol;
                    ret = _glyphs[newKey];
                    readStr = oldKey;
                    goto bottom;
                }
            }
            // ret was not found in the loop
            index++;
            if (!_glyphs.TryGetValue(c, out ret))
            {
                ret = _glyphs['?']; // Will crash if there is no '?' in this font
            }
            readStr = c.ToString();
        bottom:
            xOffset += ret.CharWidth + ret.CharSpace;
            return ret;
        }

        public void MeasureString(string str, out int width, out int height)
        {
            if (string.IsNullOrEmpty(str))
            {
                width = 0;
                height = 0;
            }
            else
            {
                width = 0;
                height = FontHeight;

                int index = 0;
                int xOffset = 0;
                while (index < str.Length)
                {
                    GetGlyph(str, ref index, ref xOffset, ref height, out _);
                    if (xOffset > width)
                    {
                        width = xOffset;
                    }
                }
            }
        }

        // A single glyph
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DrawGlyph(uint* bmpAddress, int bmpWidth, int bmpHeight, float x, float y, Glyph glyph, uint[] fontColors)
        {
            int ix = (int)(x * bmpWidth);
            int iy = (int)(y * bmpHeight);
            DrawGlyph(bmpAddress, bmpWidth, bmpHeight, ix, iy, glyph, fontColors);
        }
        public unsafe void DrawGlyph(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, Glyph glyph, uint[] fontColors)
        {
            int curBit = 0;
            int curByte = 0;
            for (int py = y; py < y + FontHeight; py++)
            {
                for (int px = x; px < x + glyph.CharWidth; px++)
                {
                    RenderUtils.DrawChecked(bmpAddress, bmpWidth, bmpHeight, px, py, fontColors[(glyph.Bitmap[curByte] >> (8 - BitsPerPixel - curBit)) % (1 << BitsPerPixel)]);
                    curBit = (curBit + BitsPerPixel) % 8;
                    if (curBit == 0)
                    {
                        curByte++;
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DrawGlyph(uint* bmpAddress, int bmpWidth, int bmpHeight, float x, float y, int scale, Glyph glyph, uint[] fontColors)
        {
            int ix = (int)(x * bmpWidth);
            int iy = (int)(y * bmpHeight);
            DrawGlyph(bmpAddress, bmpWidth, bmpHeight, ix, iy, scale, glyph, fontColors);
        }
        public unsafe void DrawGlyph(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int scale, Glyph glyph, uint[] fontColors)
        {
            if (scale <= 1)
            {
                DrawGlyph(bmpAddress, bmpWidth, bmpHeight, x, y, glyph, fontColors);
                return;
            }
            int curBit = 0;
            int curByte = 0;
            for (int py = y; py < y + (FontHeight * scale); py += scale)
            {
                for (int px = x; px < x + (glyph.CharWidth * scale); px += scale)
                {
                    uint color = fontColors[(glyph.Bitmap[curByte] >> (8 - BitsPerPixel - curBit)) % (1 << BitsPerPixel)];
                    for (int ys = 0; ys < scale; ys++)
                    {
                        for (int xs = 0; xs < scale; xs++)
                        {
                            RenderUtils.DrawChecked(bmpAddress, bmpWidth, bmpHeight, px + xs, py + ys, color);
                        }
                    }
                    curBit = (curBit + BitsPerPixel) % 8;
                    if (curBit == 0)
                    {
                        curByte++;
                    }
                }
            }
        }
        // Full string
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DrawString(uint* bmpAddress, int bmpWidth, int bmpHeight, float x, float y, string str, uint[] fontColors)
        {
            int ix = (int)(x * bmpWidth);
            int iy = (int)(y * bmpHeight);
            DrawString(bmpAddress, bmpWidth, bmpHeight, ix, iy, str, fontColors);
        }
        public unsafe void DrawString(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, string str, uint[] fontColors)
        {
            int nextXOffset = 0;
            int nextYOffset = 0;
            int index = 0;
            while (index < str.Length)
            {
                int curX = x + nextXOffset;
                int curY = y + nextYOffset;
                Glyph glyph = GetGlyph(str, ref index, ref nextXOffset, ref nextYOffset, out _);
                if (glyph != null)
                {
                    DrawGlyph(bmpAddress, bmpWidth, bmpHeight, curX, curY, glyph, fontColors);
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DrawString(uint* bmpAddress, int bmpWidth, int bmpHeight, float x, float y, int scale, string str, uint[] fontColors)
        {
            int ix = (int)(x * bmpWidth);
            int iy = (int)(y * bmpHeight);
            DrawString(bmpAddress, bmpWidth, bmpHeight, ix, iy, scale, str, fontColors);
        }
        public unsafe void DrawString(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, int scale, string str, uint[] fontColors)
        {
            if (scale <= 1)
            {
                DrawString(bmpAddress, bmpWidth, bmpHeight, x, y, str, fontColors);
                return;
            }
            int nextXOffset = 0;
            int nextYOffset = 0;
            int index = 0;
            while (index < str.Length)
            {
                int curX = (x + nextXOffset) * scale;
                int curY = (y + nextYOffset) * scale;
                Glyph glyph = GetGlyph(str, ref index, ref nextXOffset, ref nextYOffset, out _);
                if (glyph != null)
                {
                    DrawGlyph(bmpAddress, bmpWidth, bmpHeight, curX, curY, scale, glyph, fontColors);
                }
            }
        }
    }
}
