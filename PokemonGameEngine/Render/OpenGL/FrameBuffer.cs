using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal sealed class FrameBuffer
    {
        private static readonly Stack<FrameBuffer> _fbos = new();
        public static FrameBuffer Current;

        public readonly uint Id;
        public readonly Size2D Size;
        public readonly uint ColorTexture;
        public readonly uint? DepthTexture;
        public readonly uint? DepthBuffer;

        private FrameBuffer(uint id, Size2D size, uint colorTexture, uint? depthTexture, uint? depthBuffer)
        {
            Id = id;
            Size = size;
            ColorTexture = colorTexture;
            DepthTexture = depthTexture;
            DepthBuffer = depthBuffer;
        }

        public static unsafe FrameBuffer CreateWithColor(Size2D size)
        {
            GL gl = Display.OpenGL;
            uint fbo = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            gl.ActiveTexture(TextureUnit.Texture0);

            // Add color texture attachment
            uint colorTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, colorTexture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, size.Width, size.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorTexture, 0);

            return new FrameBuffer(fbo, size, colorTexture, null, null);
        }
        public static unsafe FrameBuffer CreateWithColorAndDepth(Size2D size)
        {
            GL gl = Display.OpenGL;
            uint fbo = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            gl.ActiveTexture(TextureUnit.Texture0);

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
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTexture, 0);

            // Add depth buffer attachment
            uint depthBuffer = gl.GenRenderbuffer();
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, size.Width, size.Height);
            gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthBuffer);

            return new FrameBuffer(fbo, size, colorTexture, depthTexture, depthBuffer);
        }

        public void Push()
        {
            GL gl = Display.OpenGL;
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            gl.Viewport(0, 0, Size.Width, Size.Height);
            _fbos.Push(Current);
            Current = this;
        }
        public static void Pop()
        {
            GL gl = Display.OpenGL;
            Current = _fbos.Pop();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Current.Id);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            gl.Viewport(0, 0, Current.Size.Width, Current.Size.Height);
        }

        public void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteFramebuffer(Id);
            gl.DeleteTexture(ColorTexture);
            if (DepthTexture.HasValue)
            {
                gl.DeleteTexture(DepthTexture.Value);
            }
            if (DepthBuffer.HasValue)
            {
                gl.DeleteRenderbuffer(DepthBuffer.Value);
            }
        }
    }
}
