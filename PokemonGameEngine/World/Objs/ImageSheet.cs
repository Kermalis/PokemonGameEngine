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
        public readonly Image[] Images;
        public readonly Size2D ImageSize;
        public readonly WriteableImage ShadowImage;
        public readonly Pos2D ShadowOffset;

        private unsafe ImageSheet(string id)
        {
            using (EndianBinaryReader r = GetReader())
            {
                r.BaseStream.Position = _sheetOffsets[id];
                Images = Renderer.GetResourceSheetAsImages(SheetsPath + r.ReadStringNullTerminated(), ImageSize = new Size2D(r.ReadUInt32(), r.ReadUInt32()));
                ShadowOffset = new Pos2D(r.ReadInt32(), r.ReadInt32());
                ShadowImage = new WriteableImage(new Size2D(r.ReadUInt32(), r.ReadUInt32()));
                uint dstW = ShadowImage.Size.Width;
                uint dstH = ShadowImage.Size.Height;
                uint[] bmp = new uint[dstW * dstH];
                fixed (uint* dst = bmp)
                {
                    Renderer.FillEllipse_Points(dst, dstW, dstH, 0, 0, (int)dstW - 1, (int)dstH - 1, Renderer.RawColor(0, 0, 0, 160));
                    ShadowImage.LoadTextureData(Game.OpenGL, dst);
                }
            }
            _id = id;
            _numReferences = 1;
            _loadedSheets.Add(id, this);
        }

        #region Loading

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

        #endregion

        #region Cache

        private readonly string _id;
        private int _numReferences;
        private static readonly Dictionary<string, ImageSheet> _loadedSheets = new();
        public static ImageSheet LoadOrGet(string resource)
        {
            if (_loadedSheets.TryGetValue(resource, out ImageSheet s))
            {
                s._numReferences++;
            }
            else
            {
                s = new ImageSheet(resource);
            }
            return s;
        }

        public void DeductReference(GL gl)
        {
            if (--_numReferences <= 0)
            {
                ShadowImage.DeductReference(gl);
                for (int i = 0; i < Images.Length; i++)
                {
                    Images[i].DeductReference(gl);
                }
                _loadedSheets.Remove(_id);
            }
        }

        #endregion
    }
}
