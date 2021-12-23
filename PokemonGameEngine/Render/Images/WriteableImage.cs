using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    /// <summary>Does not have a depth buffer</summary>
    internal sealed class WriteableImage : IImage
    {
        public uint Texture => FrameBuffer.ColorTexture.Value;
        public Size2D Size => FrameBuffer.Size;
        public readonly FrameBuffer FrameBuffer;

        public unsafe WriteableImage(Size2D size)
        {
            FrameBuffer = FrameBuffer.CreateWithColor(size);
        }

        public unsafe void LoadTextureData(GL gl, void* data)
        {
            FrameBuffer oldFBO = FrameBuffer.Current;
            FrameBuffer.Use();
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, Texture);
            GLTextureUtils.LoadTextureData(gl, data, Size);
            oldFBO?.Use(); // Can be null, since this runs at init (PlayerObj)
        }

        public void Render(Pos2D pos, bool xFlip = false, bool yFlip = false)
        {
            GUIRenderer.Instance.RenderTexture(Texture, new Rect2D(pos, Size), xFlip: xFlip, yFlip: !yFlip); // Flip yFlip since the GL texture is upside-down
        }

        public void DeductReference()
        {
            GL gl = Display.OpenGL;
            gl.DeleteTexture(Texture);
            FrameBuffer.Delete();
        }
    }
}
