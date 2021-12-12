using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal sealed class Image : IImage
    {
        public uint Texture { get; }
        public Size2D Size { get; }

        private Image(string asset)
        {
            AssetLoader.GetAssetBitmap(asset, out Size2D size, out uint[] bitmap);
            GL gl = Display.OpenGL;
            gl.ActiveTexture(TextureUnit.Texture0);
            Texture = gl.GenTexture();
            Size = size;
            UpdateGLTexture(gl, bitmap);
            _id = asset;
            _numReferences = 1;
            _loadedImages.Add(asset, this);
        }

        private unsafe void UpdateGLTexture(GL gl, uint[] bitmap)
        {
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, Texture);
            fixed (void* imgdata = bitmap)
            {
                GLTextureUtils.LoadTextureData(gl, imgdata, Size);
            }
        }

        public void Render(Pos2D pos, bool xFlip = false, bool yFlip = false)
        {
            GUIRenderer.Instance.RenderTexture(Texture, new Rect2D(pos, Size), xFlip: xFlip, yFlip: yFlip);
        }
        public void Render(Rect2D rect, AtlasPos part)
        {
            GUIRenderer.Instance.RenderTexture(Texture, rect, part);
        }

        #region Cache

        private readonly string _id;
        private int _numReferences;
        private static readonly Dictionary<string, Image> _loadedImages = new();
        public static Image LoadOrGet(string asset)
        {
            if (_loadedImages.TryGetValue(asset, out Image img))
            {
                img._numReferences++;
            }
            else
            {
                img = new Image(asset);
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

        #endregion
    }
}
