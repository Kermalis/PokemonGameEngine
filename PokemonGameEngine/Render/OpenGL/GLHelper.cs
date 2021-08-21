using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal static partial class GLHelper
    {
        private static readonly TextureState[] _textures;
        private static int _activeTexture;
        private static readonly DepthState _depth = new();
        private static readonly BlendState _blend = new();

        private static readonly Stack<uint> _fboWidths = new();
        private static readonly Stack<uint> _fboHeights = new();
        private static readonly Stack<uint> _fbos = new();
        public static uint CurrentWidth;
        public static uint CurrentHeight;
        private static uint _currentFBO;

        static GLHelper()
        {
            _textures = new TextureState[12];
            for (int i = 0; i < _textures.Length; i++)
            {
                _textures[i] = new TextureState();
            }
        }

        public static void ClearColor(GL gl, in ColorF color)
        {
            gl.ClearColor(color.R, color.G, color.B, color.A);
        }

        public static void PushFrameBuffer(GL gl, uint fbo, uint w, uint h)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            gl.Viewport(0, 0, w, h);
            _fboWidths.Push(CurrentWidth);
            _fboHeights.Push(CurrentHeight);
            _fbos.Push(_currentFBO);
            CurrentWidth = w;
            CurrentHeight = h;
            _currentFBO = fbo;
        }
        public static void PopFrameBuffer(GL gl)
        {
            CurrentWidth = _fboWidths.Pop();
            CurrentHeight = _fboHeights.Pop();
            _currentFBO = _fbos.Pop();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _currentFBO);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            gl.Viewport(0, 0, CurrentWidth, CurrentHeight);
        }

        public static void ActiveTexture(GL gl, TextureUnit t)
        {
            int it = t - TextureUnit.Texture0;
            if (_activeTexture != it)
            {
                _activeTexture = it;
                gl.ActiveTexture(t);
            }
        }
        public static void BindTexture(GL gl, uint texture)
        {
            TextureState t = _textures[_activeTexture];
            if (texture != t.TextureId)
            {
                t.TextureId = texture;
                gl.BindTexture(TextureTarget.Texture2D, texture);
            }
        }
        public static uint GenTexture(GL gl)
        {
            uint t = gl.GenTexture();
            if (t == 0)
            {
                throw new InvalidOperationException("Failed to create a texture");
            }
            return t;
        }

        public static void EnableDepthTest(GL gl, bool enabled)
        {
            _depth.DepthTestState.SetEnabled(gl, enabled);
        }

        public static void EnableBlend(GL gl, bool enabled)
        {
            _blend.State.SetEnabled(gl, enabled);
        }
        public static void BlendFunc(GL gl, BlendingFactor srcFactor, BlendingFactor dstFactor)
        {
            if (srcFactor != _blend.SrcFactor || dstFactor != _blend.DstFactor)
            {
                _blend.SrcFactor = srcFactor;
                _blend.DstFactor = dstFactor;
                gl.BlendFunc(srcFactor, dstFactor);
            }
        }
        public static void BlendFuncSeparate(GL gl, BlendingFactor srcFactor, BlendingFactor dstFactor, BlendingFactor srcFactorAlpha, BlendingFactor dstFactorAlpha)
        {
            if (srcFactor != _blend.SrcFactor || dstFactor != _blend.DstFactor || srcFactorAlpha != _blend.SrcFactorAlpha || dstFactorAlpha != _blend.DstFactorAlpha)
            {
                _blend.SrcFactor = srcFactor;
                _blend.DstFactor = dstFactor;
                _blend.SrcFactorAlpha = srcFactorAlpha;
                _blend.DstFactorAlpha = dstFactorAlpha;
                gl.BlendFuncSeparate(srcFactor, dstFactor, srcFactorAlpha, dstFactorAlpha);
            }
        }
    }
}
