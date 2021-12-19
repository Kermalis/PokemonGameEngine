using Kermalis.PokemonGameEngine.Render.Shaders;
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

            public Pos2D Pos;
            public RelPos2D TexCoords;
        }
        private struct QuadStruct
        {
            public const int OffsetOfPos = 0;
            public const uint SizeOf = OffsetOfPos + 2 * sizeof(int);

            public Pos2D Pos;
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
        private readonly QuadStruct[] _quadCache = new QuadStruct[4];

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
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, QuadStruct.SizeOf, (void*)QuadStruct.OffsetOfPos);

            _quadShader = new GUIQuadShader(gl);
        }

        private void RenderTextureStart(GL gl)
        {
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _texShader.Use(gl);
            _texShader.SetResolution(gl);
            gl.BindVertexArray(_texVAO);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _texVBO);
            _texShader.SetTextureUnit(gl, 0);
        }
        private unsafe void RenderOneTexture(GL gl, uint texture, Rect2D rect, AtlasPos texPos)
        {
            gl.BindTexture(TextureTarget.Texture2D, texture);
            _texCache[0].TexCoords = texPos.Start;
            _texCache[0].Pos = rect.TopLeft;
            _texCache[1].TexCoords = texPos.GetBottomLeft();
            _texCache[1].Pos = rect.GetExclusiveBottomLeft();
            _texCache[2].TexCoords = texPos.GetTopRight();
            _texCache[2].Pos = rect.GetExclusiveTopRight();
            _texCache[3].TexCoords = texPos.GetBottomRight();
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

        public void RenderTexture(uint texture, Rect2D rect, bool xFlip = false, bool yFlip = false)
        {
            GL gl = Display.OpenGL;
            RenderTextureStart(gl);
            RenderOneTexture(gl, texture, rect, new AtlasPos(xFlip, yFlip));
            RenderTextureEnd(gl);
        }
        public void RenderTexture(uint texture, Rect2D rect, AtlasPos texPos)
        {
            GL gl = Display.OpenGL;
            RenderTextureStart(gl);
            RenderOneTexture(gl, texture, rect, texPos);
            RenderTextureEnd(gl);
        }


        private void RenderQuadStart(GL gl, in Vector4 color)
        {
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _quadShader.Use(gl);
            _texShader.SetResolution(gl);
            gl.BindVertexArray(_quadVAO);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVBO);
            _quadShader.SetColor(gl, color);
        }
        private unsafe void RenderOneQuad(GL gl, Rect2D rect)
        {
            _quadCache[0].Pos = rect.TopLeft;
            _quadCache[1].Pos = rect.GetBottomLeft();
            _quadCache[2].Pos = rect.GetTopRight();
            _quadCache[3].Pos = rect.GetBottomRight();
            // Set bounds due to how gl works
            _quadCache[1].Pos.Y++;
            _quadCache[2].Pos.X++;
            _quadCache[3].Pos.X++;
            _quadCache[3].Pos.Y++;
            fixed (void* d = _quadCache)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, QuadStruct.SizeOf * 4, d, BufferUsageARB.StreamDraw);
            }
            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }
        private static void RenderQuadEnd(GL gl)
        {
            gl.Disable(EnableCap.Blend);
        }

        public void FillRectangle(in Vector4 color, Rect2D rect)
        {
            GL gl = Display.OpenGL;
            RenderQuadStart(gl, color);
            RenderOneQuad(gl, rect);
            RenderQuadEnd(gl);
        }
        public void DrawHorizontalLine_Width(in Vector4 color, int absDstX, int absDstY, uint absDstW)
        {
            GL gl = Display.OpenGL;
            RenderQuadStart(gl, color);
            RenderOneQuad(gl, new Rect2D(new Pos2D(absDstX, absDstY), new Size2D(absDstW, 1)));
            RenderQuadEnd(gl);
        }
        public void DrawVerticalLine_Height(in Vector4 color, int absDstX, int absDstY, uint absDstH)
        {
            GL gl = Display.OpenGL;
            RenderQuadStart(gl, color);
            RenderOneQuad(gl, new Rect2D(new Pos2D(absDstX, absDstY), new Size2D(1, absDstH)));
            RenderQuadEnd(gl);
        }
        public void DrawRectangle(in Vector4 color, Rect2D rect)
        {
            GL gl = Display.OpenGL;
            RenderQuadStart(gl, color);
            // The two vert lines
            RenderOneQuad(gl, new Rect2D(rect.TopLeft, new Size2D(1, rect.Size.Height)));
            RenderOneQuad(gl, new Rect2D(rect.GetTopRight(), new Size2D(1, rect.Size.Height)));
            // The two hori lines (don't overlap the vert lines)
            // TODO: This will overlap if the rect is very small
            RenderOneQuad(gl, new Rect2D(new Pos2D(rect.TopLeft.X + 1, rect.TopLeft.Y), new Size2D(rect.Size.Width - 2, 1)));
            RenderOneQuad(gl, new Rect2D(new Pos2D(rect.TopLeft.X + 1, rect.GetBottom()), new Size2D(rect.Size.Width - 2, 1)));
            RenderQuadEnd(gl);
        }
    }
}
