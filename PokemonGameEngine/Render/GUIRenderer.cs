using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render
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

        public static GUIRenderer Instance;

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

        public unsafe GUIRenderer()
        {
            GL gl = Game.OpenGL;
            _texVAO = gl.GenVertexArray();
            gl.BindVertexArray(_texVAO);

            _texVBO = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _texVBO);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, TexStruct.SizeOf, (void*)TexStruct.OffsetOfPos);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, TexStruct.SizeOf, (void*)TexStruct.OffsetOfTexCoords);

            _texShader = new GUITextureShader(gl);

            _quadVAO = gl.GenVertexArray();
            gl.BindVertexArray(_quadVAO);

            _quadVBO = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVBO);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, QuadStruct.SizeOf, (void*)QuadStruct.OffsetOfPos);

            _quadShader = new GUIQuadShader(gl);

            gl.BindVertexArray(0);
        }


        private void RenderTextureStart(GL gl)
        {
            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
            GLHelper.EnableBlend(gl, true);
            GLHelper.BlendFunc(gl, BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _texShader.Use(gl);
            _texShader.SetResolution(gl);
            gl.BindVertexArray(_texVAO);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _texVBO);
            gl.EnableVertexAttribArray(0);
            gl.EnableVertexAttribArray(1);
            _texShader.SetTextureUnit(gl, 0);
        }
        private unsafe void RenderOneTexture(GL gl, uint texture, Rect2D rect, AtlasPos texPos)
        {
            GLHelper.BindTexture(gl, texture);
            _texCache[0].TexCoords = texPos.GetTopLeft();
            _texCache[1].TexCoords = texPos.GetBottomLeft();
            _texCache[2].TexCoords = texPos.GetTopRight();
            _texCache[3].TexCoords = texPos.GetBottomRight();
            _texCache[0].Pos = rect.TopLeft;
            _texCache[1].Pos = rect.GetExclusiveBottomLeft();
            _texCache[2].Pos = rect.GetExclusiveTopRight();
            _texCache[3].Pos = rect.GetExclusiveBottomRight();
            fixed (void* d = _texCache)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, TexStruct.SizeOf * 4, d, BufferUsageARB.StreamDraw);
            }
            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }
        private static void RenderTextureEnd(GL gl)
        {
            GLHelper.EnableBlend(gl, false);
            GLHelper.BindTexture(gl, 0);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            gl.DisableVertexAttribArray(0);
            gl.DisableVertexAttribArray(1);
            gl.BindVertexArray(0);
            gl.UseProgram(0);
        }

        public void RenderTexture(uint texture, Rect2D rect, bool xFlip = false, bool yFlip = false)
        {
            GL gl = Game.OpenGL;
            RenderTextureStart(gl);
            RenderOneTexture(gl, texture, rect, new AtlasPos(xFlip, yFlip));
            RenderTextureEnd(gl);
        }
        public void RenderTexture(uint texture, Rect2D rect, AtlasPos texPos)
        {
            GL gl = Game.OpenGL;
            RenderTextureStart(gl);
            RenderOneTexture(gl, texture, rect, texPos);
            RenderTextureEnd(gl);
        }


        private void RenderQuadStart(GL gl, in ColorF color)
        {
            GLHelper.EnableBlend(gl, true);
            GLHelper.BlendFunc(gl, BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _quadShader.Use(gl);
            _texShader.SetResolution(gl);
            gl.BindVertexArray(_quadVAO);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVBO);
            gl.EnableVertexAttribArray(0);
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
            GLHelper.EnableBlend(gl, false);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            gl.DisableVertexAttribArray(0);
            gl.BindVertexArray(0);
            gl.UseProgram(0);
        }

        public void FillRectangle(in ColorF color, Rect2D rect)
        {
            GL gl = Game.OpenGL;
            RenderQuadStart(gl, color);
            RenderOneQuad(gl, rect);
            RenderQuadEnd(gl);
        }
        public void DrawHorizontalLine_Width(in ColorF color, int absDstX, int absDstY, uint absDstW)
        {
            GL gl = Game.OpenGL;
            RenderQuadStart(gl, color);
            RenderOneQuad(gl, new Rect2D(new Pos2D(absDstX, absDstY), new Size2D(absDstW, 1)));
            RenderQuadEnd(gl);
        }
        public void DrawVerticalLine_Height(in ColorF color, int absDstX, int absDstY, uint absDstH)
        {
            GL gl = Game.OpenGL;
            RenderQuadStart(gl, color);
            RenderOneQuad(gl, new Rect2D(new Pos2D(absDstX, absDstY), new Size2D(1, absDstH)));
            RenderQuadEnd(gl);
        }
        public void DrawRectangle(in ColorF color, Rect2D rect)
        {
            GL gl = Game.OpenGL;
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

        public void GameExit(GL gl)
        {
            gl.DeleteVertexArray(_texVAO);
            gl.DeleteBuffer(_texVBO);
            _texShader.Delete(gl);
            gl.DeleteVertexArray(_quadVAO);
            gl.DeleteBuffer(_quadVBO);
            _quadShader.Delete(gl);
        }
    }
}
