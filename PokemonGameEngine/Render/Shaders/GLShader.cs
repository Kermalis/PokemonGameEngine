using Kermalis.PokemonGameEngine.Core;
using Silk.NET.OpenGL;
using System;
using System.IO;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal abstract class GLShader
    {
        public readonly uint Program;

        protected GLShader(GL gl, string vertexAsset, string fragmentAsset)
        {
            Program = gl.CreateProgram();
            uint vertex = LoadShader(gl, ShaderType.VertexShader, vertexAsset);
            uint fragment = LoadShader(gl, ShaderType.FragmentShader, fragmentAsset);
            gl.AttachShader(Program, vertex);
            gl.AttachShader(Program, fragment);
            gl.LinkProgram(Program);
            gl.GetProgram(Program, ProgramPropertyARB.LinkStatus, out int status);
            if (status != 1)
            {
                throw new Exception("Program failed to link: " + gl.GetProgramInfoLog(Program));
            }
            gl.DetachShader(Program, vertex);
            gl.DetachShader(Program, fragment);
            gl.DeleteShader(vertex);
            gl.DeleteShader(fragment);
        }
        private static uint LoadShader(GL gl, ShaderType type, string asset)
        {
            string src;
            using (StreamReader sr = File.OpenText(AssetLoader.GetPath(@"Shaders\" + asset)))
            {
                src = sr.ReadToEnd();
            }

            uint handle = gl.CreateShader(type);
            gl.ShaderSource(handle, src);
            gl.CompileShader(handle);

            string error = gl.GetShaderInfoLog(handle);
            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"Error compiling \"{type}\" shader: {error}");
            }

            return handle;
        }

        public int GetUniformLocation(GL gl, string name, bool throwIfNotExists = true)
        {
            int location = gl.GetUniformLocation(Program, name);
            if (throwIfNotExists && location == -1)
            {
                throw new Exception($"\"{name}\" uniform was not found on the shader");
            }
            return location;
        }

        protected static unsafe void Matrix4(GL gl, int loc, Matrix4x4 value)
        {
            gl.UniformMatrix4(loc, 1, false, (float*)&value);
        }

        public void Use(GL gl)
        {
            gl.UseProgram(Program);
        }
        public void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteProgram(Program);
        }
    }
}