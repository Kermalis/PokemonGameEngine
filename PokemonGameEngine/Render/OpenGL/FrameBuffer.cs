using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal abstract class FrameBuffer
    {
        public readonly uint Id;

        protected FrameBuffer()
        {
            GL gl = Display.OpenGL;
            Id = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
        }

        public abstract void OnCurrent();
        public void Use()
        {
            GL gl = Display.OpenGL;
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
            OnCurrent();
        }

        public virtual void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteFramebuffer(Id);
        }
    }
}
