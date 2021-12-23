using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal sealed class FrameBuffer
    {
        public static FrameBuffer Current;

        public readonly uint Id;
        public readonly Size2D Size;
        public readonly uint? ColorTexture;
        public readonly uint? DepthTexture;

        private FrameBuffer(uint id, Size2D size, uint? colorTexture, uint? depthTexture)
        {
            Id = id;
            Size = size;
            ColorTexture = colorTexture;
            DepthTexture = depthTexture;
        }

        public static unsafe FrameBuffer CreateWithColor(Size2D size)
        {
            GL gl = Display.OpenGL;
            uint fbo = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            // Add color texture attachment
            uint colorTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, colorTexture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, size.Width, size.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorTexture, 0);

            // Done, reset bound FBO
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Current is null ? 0 : Current.Id);

            return new FrameBuffer(fbo, size, colorTexture, null);
        }
        public static unsafe FrameBuffer CreateWithColorAndDepth(Size2D size)
        {
            GL gl = Display.OpenGL;
            uint fbo = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            // Add color texture attachment
            uint colorTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, colorTexture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, size.Width, size.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorTexture, 0);

            // Add depth texture attachment
            uint depthTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, depthTexture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32, size.Width, size.Height, 0, PixelFormat.DepthComponent, PixelType.Float, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTexture, 0);

            // Done, reset bound FBO
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Current is null ? 0 : Current.Id);

            return new FrameBuffer(fbo, size, colorTexture, depthTexture);
        }

        public void Use()
        {
            GL gl = Display.OpenGL;
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
            gl.Viewport(0, 0, Size.Width, Size.Height);
            Current = this;
        }

        public void RenderToScreen()
        {
            // Maintain aspect ratio of the fbo
            Size2D windowSize = Display.GetWindowSize();
            float ratioX = windowSize.Width / (float)Size.Width;
            float ratioY = windowSize.Height / (float)Size.Height;
            float ratio = ratioX < ratioY ? ratioX : ratioY;
            int dstX = (int)((windowSize.Width - (Size.Width * ratio)) * 0.5f);
            int dstY = (int)((windowSize.Height - (Size.Height * ratio)) * 0.5f);
            int dstW = (int)(Size.Width * ratio);
            int dstH = (int)(Size.Height * ratio);

            GL gl = Display.OpenGL;
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, Id); // Read from this fbo
            gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);  // Write to screen

            gl.ClearColor(Colors.Black3);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            gl.BlitFramebuffer(0, 0, (int)Size.Width, (int)Size.Height, dstX, dstY, dstX + dstW, dstY + dstH, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id); // Done, bind this fbo to read/draw again
        }

        public void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteFramebuffer(Id);
            if (ColorTexture is not null)
            {
                gl.DeleteTexture(ColorTexture.Value);
            }
            if (DepthTexture is not null)
            {
                gl.DeleteTexture(DepthTexture.Value);
            }
            if (Current == this)
            {
                Current = null;
                gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }
    }
}
