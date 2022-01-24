using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders.Transitions;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Transitions
{
    internal sealed class FadeFromColorTransition : ITransition
    {
        private readonly float _duration;
        private float _time;

        public bool IsDone { get; private set; }

        public FadeFromColorTransition(float seconds, in Vector3 color)
        {
            _duration = seconds;

            GL gl = Display.OpenGL;
            FadeColorShader shader = FadeColorShader.Instance;
            shader.Use(gl);
            shader.SetColor(gl, color);
        }

        public static FadeFromColorTransition FromBlackStandard()
        {
            return new FadeFromColorTransition(0.5f, Colors.Black3);
        }

        public void Render(FrameBuffer2DColor target)
        {
            if (IsDone)
            {
                return;
            }

            _time += Display.DeltaTime;
            float progress = _time / _duration;
            if (progress >= 1f)
            {
                progress = 1f;
                IsDone = true;
            }

            GL gl = Display.OpenGL;
            FadeColorShader shader = FadeColorShader.Instance;
            shader.Use(gl);
            shader.SetProgress(gl, -progress + 1f);

            target.UseAndViewport(gl);
            RectMesh.Instance.Render(gl);
        }

        public void Dispose()
        {
            //
        }
    }
}
