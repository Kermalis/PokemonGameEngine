using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal sealed class Image : IImage
    {
        public uint Texture { get; }
        public Size2D Size { get; }

        private Image(string resource)
        {
            GL gl = Game.OpenGL;
            Renderer.GetResourceBitmap(resource, out Size2D size, out uint[] bitmap);
            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
            Texture = GLHelper.GenTexture(gl);
            Size = size;
            UpdateGLTexture(bitmap);
            _id = resource;
            _numReferences = 1;
            _loadedImages.Add(resource, this);
        }
        // TODO: Currently only used in GetResourceSheetAsImages
        public Image(uint[] bitmap, Size2D size, string id)
        {
            GL gl = Game.OpenGL;
            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
            Texture = GLHelper.GenTexture(gl);
            Size = size;
            UpdateGLTexture(bitmap);
            _id = id;
            _numReferences = 1;
            _loadedImages.Add(id, this);
        }

        private unsafe void UpdateGLTexture(uint[] bitmap)
        {
            GL gl = Game.OpenGL;
            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
            GLHelper.BindTexture(gl, Texture);
            fixed (void* imgdata = bitmap)
            {
                GLTextureUtils.LoadTextureData(gl, imgdata, Size.Width, Size.Height);
            }
        }

        public void Render(Pos2D pos, bool xFlip = false, bool yFlip = false)
        {
            GUIRenderer.Instance.RenderTexture(Texture, new Rect2D(pos, Size), xFlip: xFlip, yFlip: yFlip);
        }

        #region Cache

        private readonly string _id;
        private int _numReferences;
        private static readonly Dictionary<string, Image> _loadedImages = new();
        public static Image LoadOrGet(string resource)
        {
            if (_loadedImages.TryGetValue(resource, out Image img))
            {
                img._numReferences++;
            }
            else
            {
                img = new Image(resource);
            }
            return img;
        }

        public void DeductReference(GL gl)
        {
            if (--_numReferences <= 0)
            {
                gl.DeleteTexture(Texture);
                _loadedImages.Remove(_id);
            }
        }

        #endregion
    }
}
