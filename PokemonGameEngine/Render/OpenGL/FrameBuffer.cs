using Kermalis.PokemonGameEngine.Render.GUIs;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal sealed class FrameBuffer
    {
        public readonly struct FBOTexture
        {
            public readonly uint Texture;
            public readonly Vec2I Size;

            public FBOTexture(uint texture, Vec2I size)
            {
                Texture = texture;
                Size = size;
            }
        }

        public readonly uint Id;
        public readonly List<FBOTexture> ColorTextures;
        public FBOTexture DepthTexture;

        public FrameBuffer()
        {
            ColorTextures = new List<FBOTexture>();

            GL gl = Display.OpenGL;
            Id = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id); // Bind for adding textures
        }

        public unsafe FrameBuffer AddColorTexture(Vec2I size)
        {
            GL gl = Display.OpenGL;
            uint colorTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, colorTexture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)size.X, (uint)size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            FramebufferAttachment attachment = FramebufferAttachment.ColorAttachment0 + ColorTextures.Count;
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, colorTexture, 0);
            ColorTextures.Add(new FBOTexture(colorTexture, size));
            return this;
        }
        public unsafe FrameBuffer AddDepthTexture(Vec2I size)
        {
            GL gl = Display.OpenGL;
            uint depthTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, depthTexture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32, (uint)size.X, (uint)size.Y, 0, PixelFormat.DepthComponent, PixelType.Float, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTexture, 0);
            DepthTexture = new FBOTexture(depthTexture, size);
            return this;
        }

        public void Use(GL gl)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
        }
        public void UseAndViewport(GL gl, int colorTexture = 0)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
            Display.Viewport(Rect.FromSize(new Vec2I(0, 0), ColorTextures[colorTexture].Size));
        }

        public void RenderColorTexture(Vec2I pos, int colorTexture = 0, bool xFlip = false, bool yFlip = true) // Flip yFlip since rendering to a GL texture is upside-down
        {
            FBOTexture t = ColorTextures[colorTexture];
            GUIRenderer.Texture(t.Texture, Rect.FromSize(pos, t.Size), new UV(xFlip, yFlip));
        }
        /// <summary>Overwrites the contents of the screen with the read buffer</summary>
        public void BlitToScreen(int colorTexture = 0)
        {
            GL gl = Display.OpenGL;
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, Id); // Read from this fbo
            gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);  // Write to screen
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0 + colorTexture);

            gl.ClearColor(Colors.Black3);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            ref Rect dst = ref Display.ScreenRect;
            Vec2I size = ColorTextures[colorTexture].Size;
            gl.BlitFramebuffer(0, 0, size.X, size.Y,
                dst.TopLeft.X, dst.TopLeft.Y, dst.GetExclusiveRight(), dst.GetExclusiveBottom(),
                ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        }

        public void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteFramebuffer(Id);
            for (int i = 0; i < ColorTextures.Count; i++)
            {
                gl.DeleteTexture(ColorTextures[i].Texture);
            }
            gl.DeleteTexture(DepthTexture.Texture);
        }
    }
}
