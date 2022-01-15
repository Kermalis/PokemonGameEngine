using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal sealed class Image : IImage
    {
        private static readonly Dictionary<string, Image> _loadedImages = new();

        private readonly string _id;
        private int _numReferences;

        public uint Texture { get; }
        public Vec2I Size { get; }

        private Image(string assetPath)
        {
            _id = assetPath;
            _numReferences = 1;
            _loadedImages.Add(assetPath, this);

            GL gl = Display.OpenGL;
            Texture = gl.GenTexture();
            uint[] bitmap = AssetLoader.GetAssetBitmap(assetPath, out Vec2I size);
            Size = size;
            UpdateGLTexture(gl, bitmap);
        }

        private unsafe void UpdateGLTexture(GL gl, uint[] bitmap)
        {
            gl.BindTexture(TextureTarget.Texture2D, Texture);
            fixed (void* imgdata = bitmap)
            {
                GLTextureUtils.LoadTextureData(gl, imgdata, Size);
            }
        }

        public void Render(Vec2I pos, bool xFlip = false, bool yFlip = false)
        {
            GUIRenderer.Texture(Texture, Rect.FromSize(pos, Size), new UV(xFlip, yFlip));
        }

        public static Image LoadOrGet(string assetPath)
        {
            if (_loadedImages.TryGetValue(assetPath, out Image img))
            {
                img._numReferences++;
            }
            else
            {
                img = new Image(assetPath);
            }
            return img;
        }
        public void DeductReference()
        {
            if (--_numReferences <= 0)
            {
                GL gl = Display.OpenGL;
                gl.DeleteTexture(Texture);
                _loadedImages.Remove(_id);
            }
        }
    }
}
