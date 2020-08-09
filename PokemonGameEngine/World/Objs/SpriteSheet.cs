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

        private const string _sheetsExtension = ".bin";
        private const string _sheetsPath = "ObjSprites.";
        private const string _sheetsFile = _sheetsPath + "ObjSprites" + _sheetsExtension;
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
            return new EndianBinaryReader(Utils.GetResourceStream(_sheetsFile), encoding: EncodingType.UTF16);
        }

        public readonly Sprite[] Sprites;
        public readonly int SpriteWidth;
        public readonly int SpriteHeight;

        private SpriteSheet(string id)
        {
            using (EndianBinaryReader r = GetReader())
            {
                r.BaseStream.Position = _sheetOffsets[id];
                Sprites = RenderUtils.LoadSpriteSheet(_sheetsPath + r.ReadStringNullTerminated(), SpriteWidth = r.ReadInt32(), SpriteHeight = r.ReadInt32());
            }
        }

        private static readonly Dictionary<string, WeakReference<SpriteSheet>> _loadedSheets = new Dictionary<string, WeakReference<SpriteSheet>>();
        public static SpriteSheet LoadOrGet(string id)
        {
            SpriteSheet s;
            if (!_loadedSheets.ContainsKey(id))
            {
                s = new SpriteSheet(id);
                _loadedSheets.Add(id, new WeakReference<SpriteSheet>(s));
                return s;
            }
            WeakReference<SpriteSheet> w = _loadedSheets[id];
            if (w.TryGetTarget(out s))
            {
                return s;
            }
            s = new SpriteSheet(id);
            w.SetTarget(s);
            return s;
        }
    }
}
