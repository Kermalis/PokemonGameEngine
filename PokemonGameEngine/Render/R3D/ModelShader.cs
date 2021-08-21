using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal class ModelShader : GLShader
    {
        private readonly int _lProjection;
        private readonly int _lModel;
        private readonly int _lView;

        public ModelShader(GL gl)
            : this(gl, "Shaders.model_vert.glsl", "Shaders.model_frag.glsl") { }
        protected ModelShader(GL gl, string vertexResource, string fragmentResource)
            : base(gl, vertexResource, fragmentResource)
        {
            _lProjection = GetUniformLocation(gl, "projection");
            _lModel = GetUniformLocation(gl, "model");
            _lView = GetUniformLocation(gl, "view");
        }

        public void SetDiffuseTextureUnit(GL gl, int i)
        {
            int loc = GetUniformLocation(gl, "texture_diffuse" + (i + 1), throwIfNotExists: false);
            if (loc != -1)
            {
                gl.Uniform1(loc, i);
            }
        }

        public virtual void SetCamera(GL gl, Camera c)
        {
            Matrix4(gl, _lProjection, c.Projection); // Projection
            Matrix4(gl, _lView, c.CreateViewMatrix()); // View
        }
        public void SetModel(GL gl, in Matrix4x4 m)
        {
            Matrix4(gl, _lModel, m);
        }
    }
}
