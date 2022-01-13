using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.SimpleGIF;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Images
{
    internal sealed partial class AnimatedImage
    {
        private sealed partial class Instance
        {
            public sealed class Frame
            {
                public const float STAY_FOREVER = -1f;

                public readonly float SecondsVisible;
                public readonly uint Texture;

                public unsafe Frame(DecodedGIF.Frame frame, Vec2I size)
                {
                    int? d = frame.Delay;
                    SecondsVisible = d is null ? STAY_FOREVER : d.Value / 1_000f;

                    GL gl = Display.OpenGL;
                    Texture = gl.GenTexture();
                    gl.BindTexture(TextureTarget.Texture2D, Texture);
                    fixed (uint* imgdata = frame.Bitmap)
                    {
#if DEBUG_ANIMIMG_HITBOX
                        Debug_AddHitbox(imgdata, size);
#endif
                        GLTextureUtils.LoadTextureData(gl, imgdata, size);
                    }
                }

#if DEBUG_ANIMIMG_HITBOX
                private unsafe void Debug_AddHitbox(uint* imgdata, Vec2I size)
                {
                    Vec2I pos;
                    for (pos.Y = 0; pos.Y < size.Y; pos.Y++)
                    {
                        for (pos.X = 0; pos.X < size.X; pos.X++)
                        {
                            uint* p = UnsafeRenderer.GetPixelAddress(imgdata, size.X, pos);
                            if (*p != 0)
                            {
                                continue;
                            }
                            if ((pos.X == 0 && pos.Y == 0)
                                || (pos.X == 0 && pos.Y == size.Y - 1)
                                || (pos.X == size.X - 1 && pos.Y == 0)
                                || (pos.X == size.X - 1 && pos.Y == size.Y - 1))
                            {
                                *p = UnsafeRenderer.RawColor(0, 0, 255, 255); // Corners
                            }
                            else if (pos.X == 0 || pos.Y == 0
                                || pos.X == size.X - 1 || pos.Y == size.Y - 1)
                            {
                                *p = UnsafeRenderer.RawColor(255, 0, 0, 255); // Borders
                            }
                            else // Inside
                            {
                                uint a = (uint)(pos.X % 4 / 3f * 255);
                                uint b = (uint)(pos.Y % 4 / 3f * 255);
                                bool horizontal = true;
                                bool vertical = true;
                                if (horizontal && vertical)
                                {
                                    *p = UnsafeRenderer.RawColor(a, b, a, 255);
                                }
                                else if (horizontal)
                                {
                                    *p = UnsafeRenderer.RawColor(0, b, 0, 255);
                                }
                                else if (vertical)
                                {
                                    *p = UnsafeRenderer.RawColor(a, 0, a, 255);
                                }
                            }
                        }
                    }
                }
#endif
            }
        }
    }
}
