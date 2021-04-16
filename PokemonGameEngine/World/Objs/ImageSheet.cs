using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class ImageSheet
    {
        private static readonly Dictionary<string, uint> _sheetOffsets;

        private const string SheetsExtension = ".bin";
        private const string SheetsPath = "ObjSprites.";
        private const string SheetsFile = SheetsPath + "ObjSprites" + SheetsExtension;
        static ImageSheet()
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

        public readonly Image[] Images;
        public readonly int ImageWidth;
        public readonly int ImageHeight;
        public readonly Image ShadowImage;
        public readonly int ShadowXOffset;
        public readonly int ShadowYOffset;

        private unsafe ImageSheet(string id)
        {
            using (EndianBinaryReader r = GetReader())
            {
                r.BaseStream.Position = _sheetOffsets[id];
                Images = RenderUtils.LoadImageSheet(SheetsPath + r.ReadStringNullTerminated(), ImageWidth = r.ReadInt32(), ImageHeight = r.ReadInt32());
                ShadowXOffset = r.ReadInt32();
                ShadowYOffset = r.ReadInt32();
                ShadowImage = new Image(r.ReadInt32(), r.ReadInt32());
                ShadowImage.Draw((uint* bmpAddress, int bmpWidth, int bmpHeight) =>
                {
                    RenderUtils.FillEllipse_Points(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth - 1, bmpHeight - 1, RenderUtils.Color(0, 0, 0, 160));
                });
            }
        }

        private static readonly Dictionary<string, WeakReference<ImageSheet>> _loadedSheets = new Dictionary<string, WeakReference<ImageSheet>>();
        public static ImageSheet LoadOrGet(string id)
        {
            ImageSheet s;
            if (!_loadedSheets.TryGetValue(id, out WeakReference<ImageSheet> w))
            {
                s = new ImageSheet(id);
                _loadedSheets.Add(id, new WeakReference<ImageSheet>(s));
            }
            else if (!w.TryGetTarget(out s))
            {
                s = new ImageSheet(id);
                w.SetTarget(s);
            }
            return s;
        }
    }
}
