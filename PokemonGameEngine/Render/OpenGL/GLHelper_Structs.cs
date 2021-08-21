using Silk.NET.OpenGL;
using System.Diagnostics;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal static partial class GLHelper
    {
        [DebuggerDisplay("[Cap: {_cap}, Enabled: {_enabled}]")]
        private class BoolState
        {
            private readonly EnableCap _cap;
            private bool _enabled;

            public BoolState(EnableCap cap)
            {
                _cap = cap;
            }

            public void SetEnabled(GL gl, bool enabled)
            {
                if (enabled == _enabled)
                {
                    return;
                }
                _enabled = enabled;
                if (enabled)
                {
                    gl.Enable(_cap);
                }
                else
                {
                    gl.Disable(_cap);
                }
            }
        }
        [DebuggerDisplay("[TextureId: {TextureId}]")]
        private class TextureState
        {
            public uint TextureId;
        }
        [DebuggerDisplay("[DepthTestState: {DepthTestState}]")]
        private class DepthState
        {
            public readonly BoolState DepthTestState = new(EnableCap.DepthTest);
            // Can have stuff like depth mask and depth func
        }
        [DebuggerDisplay("[State: {State}]")]
        private class BlendState
        {
            public readonly BoolState State = new(EnableCap.Blend);
            public BlendingFactor SrcFactor = BlendingFactor.One;
            public BlendingFactor DstFactor = BlendingFactor.Zero;
            public BlendingFactor SrcFactorAlpha = BlendingFactor.One;
            public BlendingFactor DstFactorAlpha = BlendingFactor.Zero;
        }
    }
}
