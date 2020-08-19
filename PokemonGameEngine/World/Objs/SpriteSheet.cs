using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class SpriteSheet
    {
        private static readonly Dictionary<string, uint> _sheetOffsets;

        private const string SheetsExtension = ".bin";
        private const string SheetsPath = "ObjSprites.";
        private const string SheetsFile = SheetsPath + "ObjSprites" + SheetsExtension;
        static SpriteSheet()
        {
            using (EndianBinaryReader r = GetReader())
            {
                int count = r.ReadInt32();
                _sheetOffsets = new Dictionary<string, uint>(count);
                for (int i = 0; i < count; i++)
                {
                    _sheetOffsets.Add(r.ReadStringNullTerminated(), r.ReadUInt32());
                }
            }
        }

        private static EndianBinaryReader GetReader()
        {
            return new EndianBinaryReader(Utils.GetResourceStream(SheetsFile), encoding: EncodingType.UTF16);
        }

        public readonly Sprite[] Sprites;
        public readonly int SpriteWidth;
        public readonly int SpriteHeight;
        public readonly Sprite ShadowSprite;
        public readonly int ShadowXOffset;
        public readonly int ShadowYOffset;

        private unsafe SpriteSheet(string id)
        {
            using (EndianBinaryReader r = GetReader())
            {
                r.BaseStream.Position = _sheetOffsets[id];
                Sprites = RenderUtils.LoadSpriteSheet(SheetsPath + r.ReadStringNullTerminated(), SpriteWidth = r.ReadInt32(), SpriteHeight = r.ReadInt32());
                ShadowXOffset = r.ReadInt32();
                ShadowYOffset = r.ReadInt32();
                ShadowSprite = new Sprite(r.ReadInt32(), r.ReadInt32());
                ShadowSprite.Draw((uint* bmpAddress, int bmpWidth, int bmpHeight) =>
                {
                    RenderUtils.DrawEllipse_XY(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth - 1, bmpHeight - 1, true, RenderUtils.ToRGBA8888(0x00, 0x00, 0x00, 0xA0));
                });
            }
        }

        private static readonly Dictionary<string, WeakReference<SpriteSheet>> _loadedSheets = new Dictionary<string, WeakReference<SpriteSheet>>();
        public static SpriteSheet LoadOrGet(string id)
        {
            SpriteSheet s;
            if (!_loadedSheets.TryGetValue(id, out WeakReference<SpriteSheet> w))
            {
                s = new SpriteSheet(id);
                _loadedSheets.Add(id, new WeakReference<SpriteSheet>(s));
            }
            else if (!w.TryGetTarget(out s))
            {
                s = new SpriteSheet(id);
                w.SetTarget(s);
            }
            return s;
        }
    }
}
