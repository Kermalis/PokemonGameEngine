using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    /// <summary>Does not have a depth buffer</summary>
    internal sealed class WriteableImage : IImage
    {
        public uint Texture { get; }
        public Size2D Size { get; }
        private readonly uint _fbo;

        public unsafe WriteableImage(Size2D size)
        {
            Size = size;

            GL gl = Display.OpenGL;
            _fbo = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            gl.ActiveTexture(TextureUnit.Texture0);
            Texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, Texture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, size.Width, size.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture, 0);
        }

        public void PushFrameBuffer(GL gl)
        {
            GLHelper.PushFrameBuffer(gl, _fbo, Size.Width, Size.Height);
        }

        public unsafe void LoadTextureData(GL gl, void* data)
        {
            PushFrameBuffer(gl);
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, Texture);
            GLTextureUtils.LoadTextureData(gl, data, Size);
            GLHelper.PopFrameBuffer(gl);
        }

        public void Render(Pos2D pos, bool xFlip = false, bool yFlip = false)
        {
            GUIRenderer.Instance.RenderTexture(Texture, new Rect2D(pos, Size), xFlip: xFlip, yFlip: !yFlip); // Flip yFlip since the GL texture is upside-down
        }

        public void DeductReference()
        {
            GL gl = Display.OpenGL;
            gl.DeleteTexture(Texture);
            gl.DeleteFramebuffer(_fbo);
        }
    }
}
