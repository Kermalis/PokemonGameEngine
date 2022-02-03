using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders.World;
using Silk.NET.OpenGL;
using System.Collections.Generic;
using System.IO;

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
        public readonly FrameBuffer Shadow;
        public readonly Vec2I ShadowOffset;

        private unsafe VisualObjTexture(string id)
        {
            _id = id;
            _numReferences = 1;
            _loadedSheets.Add(id, this);

            using (EndianBinaryReader r = CreateReader())
            {
                r.BaseStream.Position = _sheetOffsets[id];
                _textureAtlas = Image.LoadOrGet(AssetLoader.GetPath(SHEETS_PATH + r.ReadStringNullTerminated()));
                ImageSize = new Vec2I(r.ReadInt32(), r.ReadInt32());
                ShadowOffset = new Vec2I(r.ReadInt32(), r.ReadInt32());
                var shadowSize = new Vec2I(r.ReadInt32(), r.ReadInt32());
                Shadow = new FrameBuffer().AddColorTexture(shadowSize);

                GL gl = Display.OpenGL;
                Shadow.UseAndViewport(gl);
                GUIRenderer.Rect(Colors.FromRGBA(0, 0, 0, 200), Rect.FromSize(new Vec2I(0, 0), shadowSize), cornerRadii: new(6)); // TODO: Specify corner radius in json
            }
        }

        private static EndianBinaryReader CreateReader()
        {
            return new EndianBinaryReader(File.OpenRead(AssetLoader.GetPath(SHEETS_FILE)), encoding: EncodingType.UTF16);
        }

        public void RenderImage(VisualObjShader shader, in Rect rect, int imgIndex, bool xFlip = false, bool yFlip = false)
        {
            GL gl = Display.OpenGL;
            shader.SetRect(gl, rect);
            shader.SetUV(gl, UV.FromAtlas(imgIndex, ImageSize, _textureAtlas.Size, xFlip: xFlip, yFlip: yFlip));
            gl.BindTexture(TextureTarget.Texture2D, _textureAtlas.Texture);
            RectMesh.Instance.Render(gl);
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
