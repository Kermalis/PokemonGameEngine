using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal static class GLTextureUtils
    {
        public static unsafe void LoadTextureData(GL gl, void* data, Size2D size)
        {
            gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, size.Width, size.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }

        public static string SaveScreenTextureAsImage(GL gl, uint texture, int w, int h, string path)
        {
            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
            GLHelper.BindTexture(gl, texture);
            var data = new Rgba32[w * h];
            gl.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data.AsSpan());
            using (var img = Image.LoadPixelData(data, w, h))
            {
                img.Mutate(x => x.Flip(FlipMode.Vertical));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                path = Path.GetFullPath(path);
                img.SaveAsPng(path);
            }
            GLHelper.BindTexture(gl, 0);
            return path;
        }
#if DEBUG
        public static string Debug_SaveFontTextureAsImage(GL gl, uint texture, int w, int h, string path)
        {
            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);
            GLHelper.BindTexture(gl, texture);
            var data = new A8[w * h];
            gl.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.RedInteger, PixelType.UnsignedByte, data.AsSpan());
            using (var img = Image.LoadPixelData(data, w, h))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                path = Path.GetFullPath(path);
                img.SaveAsPng(path);
            }
            GLHelper.BindTexture(gl, 0);
            return path;
        }
#endif
    }
}
