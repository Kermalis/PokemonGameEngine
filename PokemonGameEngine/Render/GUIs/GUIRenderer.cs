using Kermalis.PokemonGameEngine.Render.Shaders.GUIs;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal static class GUIRenderer
    {
        public static void Texture(uint texture, in Rect rect, in UV uv, float opacity = 1f)
        {
            GL gl = Display.OpenGL;
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, texture);

            GUIRectShader shader = GUIRectShader.Instance;
            shader.Use(gl);
            shader.UpdateViewport(gl, Display.ViewportSize);
            shader.SetRect(gl, rect);
            shader.SetCornerRadii(gl, default);
            shader.SetLineThickness(gl, 0);
            shader.SetOpacity(gl, opacity);
            shader.SetUseTexture(gl, true);
            shader.SetUV(gl, uv);
            RectMesh.Instance.Render(gl);
        }
        public static void Rect(in Vector4 color, in Rect rect, int lineThickness = 0, Vector4D<int> cornerRadii = default)
        {
            GL gl = Display.OpenGL;

            GUIRectShader shader = GUIRectShader.Instance;
            shader.Use(gl);
            shader.UpdateViewport(gl, Display.ViewportSize);
            shader.SetRect(gl, rect);
            shader.SetCornerRadii(gl, cornerRadii);
            shader.SetLineThickness(gl, lineThickness);
            shader.SetOpacity(gl, 1f);
            shader.SetUseTexture(gl, false);
            if (lineThickness == 0)
            {
                shader.SetColor(gl, color);
            }
            else
            {
                shader.SetColor(gl, default);
                shader.SetLineColor(gl, color);
            }
            RectMesh.Instance.Render(gl);
        }
        public static void Rect(in Vector4 color, in Vector4 lineColor, in Rect rect, int lineThickness, Vector4D<int> cornerRadii = default)
        {
            GL gl = Display.OpenGL;

            GUIRectShader shader = GUIRectShader.Instance;
            shader.Use(gl);
            shader.UpdateViewport(gl, Display.ViewportSize);
            shader.SetRect(gl, rect);
            shader.SetCornerRadii(gl, cornerRadii);
            shader.SetLineThickness(gl, lineThickness);
            shader.SetOpacity(gl, 1f);
            shader.SetUseTexture(gl, false);
            shader.SetColor(gl, color);
            shader.SetLineColor(gl, lineColor);
            RectMesh.Instance.Render(gl);
        }
    }
}
