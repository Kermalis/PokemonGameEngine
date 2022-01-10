using Kermalis.PokemonGameEngine.Render.Shaders.GUIs;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal static class GUIRenderer
    {
        public static void Texture(uint texture, in Rect rect, in UV uv)
        {
            GL gl = Display.OpenGL;
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, texture);

            GUIRectShader shader = GUIRectShader.Instance;
            shader.Use(gl);
            shader.UpdateViewport(gl, Display.ViewportSize);
            shader.SetRect(gl, rect);
            shader.SetCornerRadius(gl, 0);
            shader.SetLineThickness(gl, 0);
            shader.SetUV(gl, uv);
            RectMesh.Instance.Render(gl);

            gl.Disable(EnableCap.Blend);
        }
        public static void Rect(in Vector4 color, in Rect rect, int lineThickness = 0, int cornerRadius = 0)
        {
            GL gl = Display.OpenGL;
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GUIRectShader shader = GUIRectShader.Instance;
            shader.Use(gl);
            shader.UpdateViewport(gl, Display.ViewportSize);
            shader.SetRect(gl, rect);
            shader.SetCornerRadius(gl, cornerRadius);
            shader.SetLineThickness(gl, lineThickness);
            shader.SetColor(gl, color);
            RectMesh.Instance.Render(gl);

            gl.Disable(EnableCap.Blend);
        }
    }
}
