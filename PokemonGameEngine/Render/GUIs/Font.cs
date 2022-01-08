using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Silk.NET.OpenGL;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Render.GUIs
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
            Default = new Font("Fonts\\Default.kermfont", new Vec2I(1024, 1024), new (string, ushort)[]
            {
                ("♂", 0x246D),
                ("♀", 0x246E),
                ("[PK]", 0x2486),
                ("[MN]", 0x2487)
            });
            DefaultSmall = new Font("Fonts\\DefaultSmall.kermfont", new Vec2I(1024, 1024), Default._overrides);
            PartyNumbers = new Font("Fonts\\PartyNumbers.kermfont", new Vec2I(64, 64), new (string, ushort)[]
            {
                ("[ID]", 0x0049),
                ("[LV]", 0x004C),
                ("[NO]", 0x004E)
            });
        }

        // Atlas size must be a power of 2
        private unsafe Font(string asset, Vec2I atlasSize, (string, ushort)[] overrides)
        {
            const int SPACING = 1;
            using (var r = new EndianBinaryReader(AssetLoader.GetAssetStream(asset), Endianness.LittleEndian))
            {
                FontHeight = r.ReadByte();
                if (FontHeight > atlasSize.Y)
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
                var posInAtlas = new Vec2I(0, 0);
                foreach (KeyValuePair<ushort, PackedGlyph> k in packed)
                {
                    ushort key = k.Key;
                    PackedGlyph pg = k.Value;
                    if (pg.CharWidth > atlasSize.X)
                    {
                        throw new InvalidDataException();
                    }
                    if (posInAtlas.X >= atlasSize.X || posInAtlas.X + pg.CharWidth > atlasSize.X)
                    {
                        posInAtlas.X = 0;
                        posInAtlas.Y += FontHeight + SPACING;
                        if (posInAtlas.Y + FontHeight > atlasSize.Y)
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
                    gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.R8ui, (uint)atlasSize.X, (uint)atlasSize.Y, 0, PixelFormat.RedInteger, PixelType.UnsignedByte, dst);
                    gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                }
            }
        }

        public Glyph GetGlyph(string str, ref int index, ref Vec2I cursor, out string readStr)
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
                cursor.X = 0;
                cursor.Y += FontHeight + 1;
                readStr = c.ToString();
                return null;
            }
            if (c == '\f')
            {
                index++;
                cursor.X = 0;
                cursor.Y = 0;
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
            cursor.X += ret.CharWidth + ret.CharSpace;
            return ret;
        }

        public Vec2I GetSize(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new Vec2I(0, 0);
            }
            int index = 0;
            var cursor = new Vec2I(0, 0);
            var biggest = new Vec2I(0, 0);
            while (index < str.Length)
            {
                GetGlyph(str, ref index, ref cursor, out _);
                if (cursor.X > biggest.X)
                {
                    biggest.X = cursor.X;
                }
                if (cursor.Y > biggest.Y)
                {
                    biggest.Y = cursor.Y;
                }
            }
            biggest.Y += FontHeight;
            return biggest;
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
