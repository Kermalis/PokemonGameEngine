using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Transition
{
    internal sealed class FadeFromColorTransition : FadeColorTransition
    {
        private readonly float _duration;
        private float _time;

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

        public override void Render()
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

            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.BlendEquation(BlendEquationModeEXT.FuncAddExt);
            EntireScreenMesh.Instance.Render();
            gl.Disable(EnableCap.Blend);
        }
    }
}
