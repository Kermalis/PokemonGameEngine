using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal sealed class FrameBuffer2DColorDepth : FrameBuffer2DColor
    {
        public readonly uint DepthTexture;

        public unsafe FrameBuffer2DColorDepth(Vec2I size)
            : base(size)
        {
            GL gl = Display.OpenGL;
            // Create depth texture
            DepthTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, DepthTexture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32, (uint)size.X, (uint)size.Y, 0, PixelFormat.DepthComponent, PixelType.Float, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            // Add depth texture attachment
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, DepthTexture, 0);
        }

        public override void Delete()
        {
            base.Delete();
            Display.OpenGL.DeleteTexture(DepthTexture);
        }
    }
}
