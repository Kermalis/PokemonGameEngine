using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal sealed class VisualObjTexture
    {
        private const string SHEETS_PATH = @"ObjSprites\";
        private const string SHEETS_EXTENSION = ".bin";
        private const string SHEETS_FILE = SHEETS_PATH + "ObjSprites" + SHEETS_EXTENSION;

        private static readonly Dictionary<string, uint> _sheetOffsets;
        private static readonly Dictionary<string, VisualObjTexture> _loadedSheets = new();

        static VisualObjTexture()
        {
            using (EndianBinaryReader r = CreateReader())
            {
                int count = r.ReadInt32();
                _sheetOffsets = new Dictionary<string, uint>(count);
                for (int i = 0; i < count; i++)
                {
                    _sheetOffsets.Add(r.ReadStringNullTerminated(), r.ReadUInt32());
                }
            }
        }

        private readonly string _id;
        private int _numReferences;

        private readonly Image _textureAtlas;
        public readonly Vec2I ImageSize;
        public readonly FrameBuffer2DColor Shadow;
        public readonly Vec2I ShadowOffset;

        private unsafe VisualObjTexture(string id)
        {
            _id = id;
            _numReferences = 1;
            _loadedSheets.Add(id, this);

            using (EndianBinaryReader r = CreateReader())
            {
                r.BaseStream.Position = _sheetOffsets[id];
                _textureAtlas = Image.LoadOrGet(SHEETS_PATH + r.ReadStringNullTerminated());
                ImageSize = new Vec2I(r.ReadInt32(), r.ReadInt32());
                ShadowOffset = new Vec2I(r.ReadInt32(), r.ReadInt32());
                var shadowSize = new Vec2I(r.ReadInt32(), r.ReadInt32());
                Shadow = new FrameBuffer2DColor(shadowSize);

                Shadow.Use();
                GL gl = Display.OpenGL;
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, Shadow.ColorTexture);
                GUIRenderer.Rect(Colors.FromRGBA(0, 0, 0, 200), Rect.FromSize(new Vec2I(0, 0), shadowSize), cornerRadius: 6); // TODO: Specify corner radius for each
            }
        }

        private static EndianBinaryReader CreateReader()
        {
            return new EndianBinaryReader(AssetLoader.GetAssetStream(SHEETS_FILE), encoding: EncodingType.UTF16);
        }

        public void RenderImage(in Rect rect, int imgIndex, bool xFlip = false, bool yFlip = false)
        {
            _textureAtlas.Render(rect, UV.FromAtlas(imgIndex, ImageSize, _textureAtlas.Size, xFlip: xFlip, yFlip: yFlip));
        }

        public static VisualObjTexture LoadOrGet(string asset)
        {
            if (_loadedSheets.TryGetValue(asset, out VisualObjTexture s))
            {
                s._numReferences++;
            }
            else
            {
                s = new VisualObjTexture(asset);
            }
            return s;
        }
        public void DeductReference()
        {
            if (--_numReferences <= 0)
            {
                Shadow.Delete();
                _textureAtlas.DeductReference();
                _loadedSheets.Remove(_id);
            }
        }
    }
}
