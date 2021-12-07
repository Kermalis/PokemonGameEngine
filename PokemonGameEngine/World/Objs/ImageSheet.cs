using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Images;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class ImageSheet
    {
        public readonly Image Images;
        public readonly Size2D ImageSize;
        public readonly WriteableImage ShadowImage;
        public readonly Size2D ShadowSize;
        public readonly Pos2D ShadowOffset;

        private unsafe ImageSheet(string id)
        {
            using (EndianBinaryReader r = GetReader())
            {
                r.BaseStream.Position = _sheetOffsets[id];
                Images = Image.LoadOrGet(SheetsPath + r.ReadStringNullTerminated());
                ImageSize = new Size2D(r.ReadUInt32(), r.ReadUInt32());
                ShadowOffset = new Pos2D(r.ReadInt32(), r.ReadInt32());
                ShadowSize = new Size2D(r.ReadUInt32(), r.ReadUInt32());
                Size2D shadowSizePOT = ShadowSize.PowerOfTwoize();
                ShadowImage = new WriteableImage(shadowSizePOT);
                uint[] bmp = new uint[shadowSizePOT.GetArea()];
                fixed (uint* dst = bmp)
                {
                    Renderer.FillEllipse_Points(dst, shadowSizePOT, new Pos2D(0, 0), new Pos2D((int)ShadowSize.Width - 1, (int)ShadowSize.Height - 1), Renderer.RawColor(0, 0, 0, 160));
                    ShadowImage.LoadTextureData(Game.OpenGL, dst);
                }
            }
            _id = id;
            _numReferences = 1;
            _loadedSheets.Add(id, this);
        }

        public AtlasPos GetAtlasPos(uint imgIndex, bool xFlip = false, bool yFlip = false)
        {
            return new AtlasPos(Rect2D.FromSheet(imgIndex, ImageSize, Images.Size.Width), Images.Size, xFlip: xFlip, yFlip: yFlip);
        }

        #region Loading

        private static readonly Dictionary<string, uint> _sheetOffsets;

        private const string SheetsExtension = ".bin";
        private const string SheetsPath = "ObjSprites\\";
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
            return new EndianBinaryReader(AssetLoader.GetAssetStream(SheetsFile), encoding: EncodingType.UTF16);
        }

        #endregion

        #region Cache

        private readonly string _id;
        private int _numReferences;
        private static readonly Dictionary<string, ImageSheet> _loadedSheets = new();
        public static ImageSheet LoadOrGet(string asset)
        {
            if (_loadedSheets.TryGetValue(asset, out ImageSheet s))
            {
                s._numReferences++;
            }
            else
            {
                s = new ImageSheet(asset);
            }
            return s;
        }

        public void DeductReference(GL gl)
        {
            if (--_numReferences <= 0)
            {
                ShadowImage.DeductReference(gl);
                Images.DeductReference(gl);
                _loadedSheets.Remove(_id);
            }
        }

        #endregion
    }
}
