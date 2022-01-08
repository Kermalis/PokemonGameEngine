using Kermalis.PokemonGameEngine.Render.Shaders.GUIs;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    // TODO: Optimize
    internal sealed class GUIRenderer
    {
        #region Structs

        private struct TexStruct
        {
            public const int OffsetOfPos = 0;
            public const int OffsetOfTexCoords = OffsetOfPos + 2 * sizeof(int);
            public const uint SizeOf = OffsetOfTexCoords + (2 * sizeof(float));

            public Vec2I Pos;
            public Vector2 UV;
        }

        #endregion

        public static GUIRenderer Instance { get; private set; } = null!; // Set in RenderManager

        private readonly uint _texVAO;
        private readonly uint _texVBO;
        private readonly GUITextureShader _texShader;
        /// <summary>Top left, bottom left, top right, bottom right</summary>
        private readonly TexStruct[] _texCache = new TexStruct[4];

        private readonly uint _quadVAO;
        private readonly uint _quadVBO;
        private readonly GUIQuadShader _quadShader;
        /// <summary>Top left, bottom left, top right, bottom right</summary>
        private readonly Vec2I[] _quadCache = new Vec2I[4];

        public unsafe GUIRenderer(GL gl)
        {
            Instance = this;

            _texVAO = gl.GenVertexArray();
            gl.BindVertexArray(_texVAO);

            _texVBO = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _texVBO);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, TexStruct.SizeOf, (void*)TexStruct.OffsetOfPos);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, TexStruct.SizeOf, (void*)TexStruct.OffsetOfTexCoords);

            _texShader = new GUITextureShader(gl);

            _quadVAO = gl.GenVertexArray();
            gl.BindVertexArray(_quadVAO);

            _quadVBO = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVBO);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vec2I), null);

            _quadShader = new GUIQuadShader(gl);
        }

        private void RenderTextureStart(GL gl)
        {
            _texShader.Use(gl);
            _texShader.UpdateViewport(gl, Display.ViewportSize);
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindVertexArray(_texVAO);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _texVBO); // Bind buffer so we can BufferData
        }
        private unsafe void RenderOneTexture(GL gl, uint texture, in Rect rect, in UV uv)
        {
            gl.BindTexture(TextureTarget.Texture2D, texture);
            _texCache[0].UV = uv.Start;
            _texCache[0].Pos = rect.TopLeft;
            _texCache[1].UV = uv.GetBottomLeft();
            _texCache[1].Pos = rect.GetExclusiveBottomLeft();
            _texCache[2].UV = uv.GetTopRight();
            _texCache[2].Pos = rect.GetExclusiveTopRight();
            _texCache[3].UV = uv.End;
            _texCache[3].Pos = rect.GetExclusiveBottomRight();
            fixed (void* d = _texCache)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, TexStruct.SizeOf * 4, d, BufferUsageARB.StreamDraw);
            }
            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }
        private static void RenderTextureEnd(GL gl)
        {
            gl.Disable(EnableCap.Blend);
        }

        public void RenderTexture(uint texture, in Rect rect, bool xFlip = false, bool yFlip = false)
        {
            GL gl = Display.OpenGL;
            RenderTextureStart(gl);
            RenderOneTexture(gl, texture, rect, new UV(xFlip, yFlip));
            RenderTextureEnd(gl);
        }
        public void RenderTexture(uint texture, in Rect rect, in UV uv)
        {
            GL gl = Display.OpenGL;
            RenderTextureStart(gl);
            RenderOneTexture(gl, texture, rect, uv);
            RenderTextureEnd(gl);
        }


        private void RenderQuadStart(GL gl, in Vector4 color)
        {
            _quadShader.Use(gl);
            _quadShader.UpdateViewport(gl, Display.ViewportSize);
            _quadShader.SetColor(gl, color);
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.BindVertexArray(_quadVAO);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVBO); // Bind buffer so we can BufferData
        }
        private unsafe void RenderOneQuad(GL gl, in Rect rect)
        {
            _quadCache[0] = rect.TopLeft;
            _quadCache[1] = rect.GetExclusiveBottomLeft();
            _quadCache[2] = rect.GetExclusiveTopRight();
            _quadCache[3] = rect.GetExclusiveBottomRight();
            fixed (void* d = _quadCache)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)sizeof(Vec2I) * 4, d, BufferUsageARB.StreamDraw);
            }
            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }
        private static void RenderQuadEnd(GL gl)
        {
            gl.Disable(EnableCap.Blend);
        }

        public void FillRectangle(in Vector4 color, in Rect rect)
        {
            GL gl = Display.OpenGL;
            RenderQuadStart(gl, color);
            RenderOneQuad(gl, rect);
            RenderQuadEnd(gl);
        }
        public void DrawHorizontalLine_Width(in Vector4 color, Vec2I absDstPos, int absDstW)
        {
            GL gl = Display.OpenGL;
            RenderQuadStart(gl, color);
            RenderOneQuad(gl, Rect.FromSize(absDstPos, new Vec2I(absDstW, 1)));
            RenderQuadEnd(gl);
        }
        public void DrawVerticalLine_Height(in Vector4 color, Vec2I absDstPos, int absDstH)
        {
            GL gl = Display.OpenGL;
            RenderQuadStart(gl, color);
            RenderOneQuad(gl, Rect.FromSize(absDstPos, new Vec2I(1, absDstH)));
            RenderQuadEnd(gl);
        }
        public void DrawRectangle(in Vector4 color, in Rect rect)
        {
            GL gl = Display.OpenGL;
            RenderQuadStart(gl, color);
            Vec2I size = rect.GetSize();
            // The two vert lines
            RenderOneQuad(gl, Rect.FromSize(rect.TopLeft, new Vec2I(1, size.Y)));
            RenderOneQuad(gl, Rect.FromSize(new Vec2I(rect.BottomRight.X, rect.TopLeft.Y), new Vec2I(1, size.Y)));
            // The two hori lines (don't overlap the vert lines)
            // TODO: This will overlap if the rect is very small
            RenderOneQuad(gl, Rect.FromSize(new Vec2I(rect.TopLeft.X + 1, rect.TopLeft.Y), new Vec2I(size.X - 2, 1)));
            RenderOneQuad(gl, Rect.FromSize(new Vec2I(rect.TopLeft.X + 1, rect.BottomRight.Y), new Vec2I(size.X - 2, 1)));
            RenderQuadEnd(gl);
        }
    }
}
