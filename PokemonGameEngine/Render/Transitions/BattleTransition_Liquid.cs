using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Kermalis.PokemonGameEngine.Render.Shaders.Transitions;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Transitions
{
    internal sealed class BattleTransition_Liquid : ITransition
    {
        private readonly FrameBuffer _frameBuffer;
        private readonly float _duration;
        private float _time;

        public bool IsDone { get; private set; }

        public BattleTransition_Liquid(Vec2I size, float duration = 2.5f)
        {
            _frameBuffer = new FrameBuffer().AddColorTexture(size);
            _duration = duration;
        }

        public void Render(FrameBuffer target)
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
            gl.Disable(EnableCap.Blend);
            gl.ActiveTexture(TextureUnit.Texture0);

            BattleTransitionShader_Liquid shader = BattleTransitionShader_Liquid.Instance;
            shader.Use(gl);
            shader.SetProgress(gl, progress);

            // Render to transition texture
            _frameBuffer.UseAndViewport(gl);
            gl.BindTexture(TextureTarget.Texture2D, target.ColorTextures[0].Texture);
            RectMesh.Instance.Render(gl);

            // Copy rendered result back to the target
            EntireScreenTextureShader.Instance.Use(gl);
            target.UseAndViewport(gl);
            gl.BindTexture(TextureTarget.Texture2D, _frameBuffer.ColorTextures[0].Texture);
            RectMesh.Instance.Render(gl);

            gl.Enable(EnableCap.Blend); // Re-enable blend
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public void Dispose()
        {
            _frameBuffer.Delete();
        }
    }
}
