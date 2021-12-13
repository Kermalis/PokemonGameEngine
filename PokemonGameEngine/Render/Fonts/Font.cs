using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Silk.NET.OpenGL;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Render.Fonts
{
    internal sealed class Font
    {
        public readonly byte FontHeight;
        public readonly byte BitsPerPixel;

        public readonly uint Texture;

        private readonly Dictionary<ushort, Glyph> _glyphs;
        private readonly (string OldKey, ushort NewKey)[] _overrides;

        public static Font Default { get; private set; } = null!; // Initialized in Init()
        public static Font DefaultSmall { get; private set; } = null!; // Initialized in Init()
        public static Font PartyNumbers { get; private set; } = null!; // Initialized in Init()

        public static void Init()
        {
            Default = new Font("Fonts\\Default.kermfont", new Size2D(1024, 1024), new (string, ushort)[]
            {
                ("♂", 0x246D),
                ("♀", 0x246E),
                ("[PK]", 0x2486),
                ("[MN]", 0x2487)
            });
            DefaultSmall = new Font("Fonts\\DefaultSmall.kermfont", new Size2D(1024, 1024), Default._overrides);
            PartyNumbers = new Font("Fonts\\PartyNumbers.kermfont", new Size2D(64, 64), new (string, ushort)[]
            {
                ("[ID]", 0x0049),
                ("[LV]", 0x004C),
                ("[NO]", 0x004E)
            });
        }

        // Atlas size must be a power of 2
        private unsafe Font(string asset, Size2D atlasSize, (string, ushort)[] overrides)
        {
            const int SPACING = 1;
            using (var r = new EndianBinaryReader(AssetLoader.GetAssetStream(asset), Endianness.LittleEndian))
            {
                FontHeight = r.ReadByte();
                if (FontHeight > atlasSize.Height)
                {
                    throw new InvalidDataException();
                }
                BitsPerPixel = r.ReadByte();
                int numGlyphs = r.ReadInt32();
                var packed = new Dictionary<ushort, PackedGlyph>(numGlyphs);
                for (int i = 0; i < numGlyphs; i++)
                {
                    packed.Add(r.ReadUInt16(), new PackedGlyph(r, this));
                }
                _overrides = overrides;

                // Make texture atlas. Atlas must be sized by powers of 2
                byte[] dest = new byte[atlasSize.GetArea()];
                _glyphs = new Dictionary<ushort, Glyph>(numGlyphs);
                var posInAtlas = new Pos2D(0, 0);
                foreach (KeyValuePair<ushort, PackedGlyph> k in packed)
                {
                    ushort key = k.Key;
                    PackedGlyph pg = k.Value;
                    if (pg.CharWidth > atlasSize.Width)
                    {
                        throw new InvalidDataException();
                    }
                    if (posInAtlas.X >= atlasSize.Width || posInAtlas.X + pg.CharWidth > atlasSize.Width)
                    {
                        posInAtlas.X = 0;
                        posInAtlas.Y += FontHeight + SPACING;
                        if (posInAtlas.Y + FontHeight > atlasSize.Height)
                        {
                            throw new InvalidDataException();
                        }
                    }
                    var g = new Glyph(dest, posInAtlas, atlasSize, this, pg);
                    _glyphs.Add(key, g);
                    posInAtlas.X += g.CharWidth + SPACING;
                }

                // Create the texture
                GL gl = Display.OpenGL;
                fixed (byte* dst = dest)
                {
                    gl.ActiveTexture(TextureUnit.Texture0);
                    Texture = gl.GenTexture();
                    gl.BindTexture(TextureTarget.Texture2D, Texture);
                    gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.R8ui, atlasSize.Width, atlasSize.Height, 0, PixelFormat.RedInteger, PixelType.UnsignedByte, dst);
                    gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                }
            }
        }

        public Glyph GetGlyph(string str, ref int index, ref uint xOffset, ref uint yOffset, out string readStr)
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
                yOffset += (uint)FontHeight + 1;
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
            xOffset += (uint)ret.CharWidth + ret.CharSpace;
            return ret;
        }

        public Size2D MeasureString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new Size2D(0, 0);
            }
            var s = new Size2D(0, FontHeight);
            int index = 0;
            uint xOffset = 0;
            while (index < str.Length)
            {
                GetGlyph(str, ref index, ref xOffset, ref s.Height, out _);
                if (xOffset > s.Width)
                {
                    s.Width = xOffset;
                }
            }
            return s;
        }

        private void Delete(GL gl)
        {
            gl.DeleteTexture(Texture);
        }
        public static void Quit(GL gl)
        {
            Default.Delete(gl);
            DefaultSmall.Delete(gl);
            PartyNumbers.Delete(gl);
        }
    }
}
