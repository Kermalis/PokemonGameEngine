using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal class FrameBuffer2DColor : FrameBuffer
    {
        public readonly Vec2I Size;
        public readonly uint ColorTexture;

        public unsafe FrameBuffer2DColor(Vec2I size)
            : base()
        {
            Size = size;

            GL gl = Display.OpenGL;
            // Create color texture
            ColorTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, ColorTexture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)size.X, (uint)size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            // Add color texture attachment
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorTexture, 0);
        }

        public void UseAndViewport(GL gl)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
            Display.Viewport(Rect.FromSize(new Vec2I(0, 0), Size));
        }

        public void RenderColorTexture(Vec2I pos, bool xFlip = false, bool yFlip = true) // Flip yFlip since rendering to a GL texture is upside-down
        {
            GUIRenderer.Texture(ColorTexture, Rect.FromSize(pos, Size), new UV(xFlip, yFlip));
        }
        /// <summary>Overwrites the contents of the screen with this FBO's color texture</summary>
        public void BlitToScreen()
        {
            GL gl = Display.OpenGL;
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, Id); // Read from this fbo
            gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);  // Write to screen

            gl.ClearColor(Colors.Black3);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            ref Rect dst = ref Display.ScreenRect;
            gl.BlitFramebuffer(0, 0, Size.X, Size.Y,
                dst.TopLeft.X, dst.TopLeft.Y, dst.GetExclusiveRight(), dst.GetExclusiveBottom(),
                ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        }
        /// <summary>Renders this FBO's color texture on top of the screen</summary>
        public void RenderToScreen()
        {
            GL gl = Display.OpenGL;
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Display.Viewport(Display.ScreenRect);

            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, ColorTexture);
            EntireScreenTextureShader.Instance.Use(gl);
            RectMesh.Instance.Render(gl);
        }

        public override void Delete()
        {
            base.Delete();
            Display.OpenGL.DeleteTexture(ColorTexture);
        }
    }
}
