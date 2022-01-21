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

        public abstract void SetViewport();
        public void Use(GL gl)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
            SetViewport();
        }
        public void UseNoViewport(GL gl)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
        }

        public virtual void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteFramebuffer(Id);
        }
    }
}
