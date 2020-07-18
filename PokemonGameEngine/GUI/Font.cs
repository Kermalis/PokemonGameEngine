using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Util;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class Font
    {
        public sealed class Glyph
        {
            public Font Parent { get; }

            public byte CharWidth { get; }
            public byte CharSpace { get; }
            public ReadOnlyCollection<byte> Bitmap { get; }

            public Glyph(EndianBinaryReader r, Font parent)
            {
                Parent = parent;

                CharWidth = r.ReadByte();
                CharSpace = r.ReadByte();
                int numBitsToRead = parent.FontHeight * CharWidth * parent.BitsPerPixel;
                Bitmap = new ReadOnlyCollection<byte>(r.ReadBytes((numBitsToRead / 8) + ((numBitsToRead % 8) != 0 ? 1 : 0)));
            }
        }
        public byte FontHeight { get; }
        public byte BitsPerPixel { get; }
        private readonly Dictionary<ushort, Glyph> _glyphs;
        private readonly (string OldKey, ushort NewKey)[] _overrides;

        public static Font Default { get; }
        public static uint[] DefaultWhite { get; } = new uint[] { 0x00000000, 0xFFEFEFEF, 0xFF848484 };
        public static uint[] DefaultDark { get; } = new uint[] { 0x00000000, 0xFF5A5252, 0xFFA5A5AD };
        public static uint[] DefaultMale { get; } = new uint[] { 0x00000000, 0xFF7394FF, 0xFF0000D6 };
        public static uint[] DefaultFemale { get; } = new uint[] { 0x00000000, 0xFFFF7373, 0xFFC60000 };
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
    }
}
