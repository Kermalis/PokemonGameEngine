using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers;
using System.IO;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal static class GLTextureUtils
    {
        public const int MAX_ACTIVE_TEXTURES = 16; // If this is changed, you must also change the value in the shader: World\BlocksetBlock.frag.glsl

        public static unsafe void LoadTextureData(GL gl, string assetPath, out Vec2I size)
        {
            using (FileStream s = File.OpenRead(assetPath))
            using (var img = Image.Load<Rgba32>(s))
            {
                size.X = img.Width;
                size.Y = img.Height;
                if (!img.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory))
                {
                    throw new Exception("Failure pinning memory for image");
                }
                using (MemoryHandle h = memory.Pin())
                {
                    LoadTextureData(gl, h.Pointer, size);
                }
            }
        }
        public static unsafe void LoadTextureData(GL gl, void* data, Vec2I size)
        {
            gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)size.X, (uint)size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }

        public static string SaveReadBufferAsImage(GL gl, Vec2I size, string path)
        {
            var data = new Rgb24[size.GetArea()];
            gl.ReadPixels(0, 0, (uint)size.X, (uint)size.Y, PixelFormat.Rgb, PixelType.UnsignedByte, data.AsSpan());
            using (var img = Image.LoadPixelData(data, size.X, size.Y))
            {
                img.Mutate(x => x.Flip(FlipMode.Vertical));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                path = Path.GetFullPath(path);
                img.SaveAsPng(path);
            }
            return path;
        }
#if DEBUG
        public static string Debug_SaveFontTextureAsImage(GL gl, uint texture, Vec2I size, string path)
        {
            gl.BindTexture(TextureTarget.Texture2D, texture);
            var data = new A8[size.GetArea()];
            gl.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.RedInteger, PixelType.UnsignedByte, data.AsSpan());
            using (var img = Image.LoadPixelData(data, size.X, size.Y))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                path = Path.GetFullPath(path);
                img.SaveAsPng(path);
            }
            return path;
        }
#endif
    }
}
