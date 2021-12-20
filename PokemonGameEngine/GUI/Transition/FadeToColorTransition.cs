using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class FadeToColorTransition : FadeColorTransition
    {
        private readonly float _duration;
        private float _time;

        public FadeToColorTransition(float seconds, in Vector3 color)
        {
            _duration = seconds;

            GL gl = Display.OpenGL;
            FadeColorShader shader = FadeColorShader.Instance;
            shader.Use(gl);
            shader.SetColor(gl, color);
        }

        public static FadeToColorTransition ToBlackStandard()
        {
            return new FadeToColorTransition(0.5f, Colors.Black3);
        }

        public override void Render()
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
            FadeColorShader shader = FadeColorShader.Instance;
            shader.Use(gl);
            shader.SetProgress(gl, progress);

            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.BlendEquation(BlendEquationModeEXT.FuncAddExt);
            EntireScreenMesh.Instance.Render();
            gl.Disable(EnableCap.Blend);
        }
    }
}
