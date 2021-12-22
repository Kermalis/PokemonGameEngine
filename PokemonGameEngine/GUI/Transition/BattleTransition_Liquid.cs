using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class BattleTransition_Liquid : ITransition
    {
        private readonly FrameBuffer _frameBuffer;
        private readonly float _duration;
        private float _time;

        public bool IsDone { get; private set; }

        public BattleTransition_Liquid(float duration = 2.5f)
        {
            _frameBuffer = FrameBuffer.CreateWithColor(FrameBuffer.Current.Size); // Use current fbo's size
            _duration = duration;
        }

        public void Render()
        {
            float progress;
            if (IsDone)
            {
                progress = 1f;
            }
            else
            {
                _time += Display.DeltaTime;
                progress = _time / _duration;
                if (progress >= 1f)
                {
                    progress = 1f;
                    IsDone = true;
                }
            }

            GL gl = Display.OpenGL;
            BattleTransitionShader_Liquid shader = BattleTransitionShader_Liquid.Instance;
            shader.Use(gl);
            shader.SetProgress(gl, progress);

            // Bind current fbo's texture
            FrameBuffer c = FrameBuffer.Current;
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, c.ColorTexture);

            // Render to DayTint fbo
            _frameBuffer.Use();
            EntireScreenMesh.Instance.Render();

            // Copy rendered result back to the previous fbo (its texture is still bound)
            gl.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, c.Size.Width, c.Size.Height);
            c.Use();
        }

        public void Dispose()
        {
            _frameBuffer.Delete();
        }
    }
}
