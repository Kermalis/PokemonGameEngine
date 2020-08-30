using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System.Collections.Generic;

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
        public static uint[] DefaultWhite { get; } = new uint[] { RenderUtils.Color(0x00, 0x00, 0x00, 0x00), RenderUtils.Color(0xEF, 0xEF, 0xEF, 0xFF), RenderUtils.Color(0x84, 0x84, 0x84, 0xFF) };
        public static uint[] DefaultSelected { get; } = new uint[] { RenderUtils.Color(0x00, 0x00, 0x00, 0x00), RenderUtils.Color(0xFF, 0xE0, 0x16, 0xFF), RenderUtils.Color(0xBC, 0xA5, 0x10, 0xFF) };
        public static uint[] DefaultDisabled { get; } = new uint[] { RenderUtils.Color(0x00, 0x00, 0x00, 0x00), RenderUtils.Color(0x85, 0x85, 0x8D, 0xFF), RenderUtils.Color(0x3A, 0x32, 0x32, 0xFF) };
        public static uint[] DefaultDark { get; } = new uint[] { RenderUtils.Color(0x00, 0x00, 0x00, 0x00), RenderUtils.Color(0x5A, 0x52, 0x52, 0xFF), RenderUtils.Color(0xA5, 0xA5, 0xAD, 0xFF) };
        public static uint[] DefaultMale { get; } = new uint[] { RenderUtils.Color(0x00, 0x00, 0x00, 0x00), RenderUtils.Color(0x73, 0x94, 0xFF, 0xFF), RenderUtils.Color(0x00, 0x00, 0xD6, 0xFF) };
        public static uint[] DefaultFemale { get; } = new uint[] { RenderUtils.Color(0x00, 0x00, 0x00, 0x00), RenderUtils.Color(0xFF, 0x73, 0x73, 0xFF), RenderUtils.Color(0xC6, 0x00, 0x00, 0xFF) };
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

        public Glyph GetGlyph(string str, ref int index, ref int xOffset, ref int yOffset)
        {
            char c = str[index];
            if (c == '\r') // Completely ignore CR
            {
                index++;
                return null;
            }
            else if (c == '\n')
            {
                index++;
                xOffset = 0;
                yOffset += FontHeight + 1;
                return null;
            }
            else
            {
                Glyph ret = null;
                for (int i = 0; i < _overrides.Length; i++)
                {
                    (string oldKey, ushort newKey) = _overrides[i];
                    int ol = oldKey.Length;
                    if (index + ol <= str.Length && str.Substring(index, ol) == oldKey)
                    {
                        index += ol;
                        ret = _glyphs[newKey];
                        break;
                    }
                }
                if (ret == null)
                {
                    index++;
                    if (!_glyphs.TryGetValue(c, out ret))
                    {
                        ret = _glyphs['?']; // Will crash if there is no '?' in this font
                    }
                }
                xOffset += ret.CharWidth + ret.CharSpace;
                return ret;
            }
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
                    GetGlyph(str, ref index, ref xOffset, ref height);
                    if (xOffset > width)
                    {
                        width = xOffset;
                    }
                }
            }
        }

        // A single glyph
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
        // Full string
        public unsafe void DrawString(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, string str, uint[] fontColors)
        {
            int nextXOffset = 0;
            int nextYOffset = 0;
            int index = 0;
            while (index < str.Length)
            {
                int curX = x + nextXOffset;
                int curY = y + nextYOffset;
                Glyph glyph = GetGlyph(str, ref index, ref nextXOffset, ref nextYOffset);
                if (glyph != null)
                {
                    DrawGlyph(bmpAddress, bmpWidth, bmpHeight, curX, curY, glyph, fontColors);
                }
            }
        }
    }

    internal sealed class StringPrinter
    {
        private readonly string _str;
        private readonly Font _font;
        private readonly uint[] _fontColors;
        private readonly int _startX;
        private readonly int _startY;
        private int _nextXOffset;
        private int _nextYOffset;
        private int _index;

        public StringPrinter(string str, int x, int y, Font font, uint[] fontColors)
        {
            _str = str;
            _startX = x;
            _startY = y;
            _font = font;
            _fontColors = fontColors;
        }

        public unsafe bool DrawNext(uint* bmpAddress, int bmpWidth, int bmpHeight, int count)
        {
            int i = 0;
            while (i < count && _index < _str.Length)
            {
                int curX = _startX + _nextXOffset;
                int curY = _startY + _nextYOffset;
                Font.Glyph glyph = _font.GetGlyph(_str, ref _index, ref _nextXOffset, ref _nextYOffset);
                if (glyph != null)
                {
                    _font.DrawGlyph(bmpAddress, bmpWidth, bmpHeight, curX, curY, glyph, _fontColors);
                    i++;
                }
            }
            return _index >= _str.Length;
        }
    }
}
