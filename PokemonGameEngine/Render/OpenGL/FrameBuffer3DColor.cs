using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal sealed class FrameBuffer3DColor
    {
        public readonly uint Id;
        public readonly Vec2I Size;
        public uint NumLayers;
        public uint ColorTexture;

        public FrameBuffer3DColor(Vec2I size, uint numLayers)
        {
            Size = size;
            GL gl = Display.OpenGL;
            Id = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
            UpdateTexture(numLayers);
        }

        public void UseAndViewport(GL gl)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
            Display.Viewport(Rect.FromSize(new Vec2I(0, 0), Size));
        }

        public unsafe void UpdateTexture(uint numLayers)
        {
            NumLayers = numLayers;
            GL gl = Display.OpenGL;
            gl.DeleteTexture(ColorTexture); // Delete old one if it exists
            // Create color texture
            ColorTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture3D, ColorTexture);
            gl.TexImage3D(TextureTarget.Texture3D, 0, InternalFormat.Rgba, (uint)Size.X, (uint)Size.Y, numLayers, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            gl.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        }

        public void SetLayer(int layer)
        {
            GL gl = Display.OpenGL;
            gl.FramebufferTexture3D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture3D, ColorTexture, 0, layer);
        }

        public void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteFramebuffer(Id);
            Display.OpenGL.DeleteTexture(ColorTexture);
        }
    }
}
