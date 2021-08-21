using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe partial class Renderer
    {
        public static TextureUnit ToTextureUnit(this int unit)
        {
            return (TextureUnit)((int)TextureUnit.Texture0 + unit);
        }

        #region Images

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AbsXToRelX(float x)
        {
            return x / GLHelper.CurrentWidth;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AbsXToRelX(float x, uint totalWidth)
        {
            return x / totalWidth;
        }
        /// <summary>0 -> -1, 1 -> 1</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RelXToGLX(float x)
        {
            return (x * 2) - 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RelXToAbsX(float x)
        {
            return (int)(x * GLHelper.CurrentWidth);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AbsYToRelY(float y)
        {
            return y / GLHelper.CurrentHeight;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AbsYToRelY(float y, uint totalHeight)
        {
            return y / totalHeight;
        }
        /// <summary>0 -> 1, 1 -> -1</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RelYToGLY(float y)
        {
            return (y * -2) + 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RelYToAbsY(float y)
        {
            return (int)(y * GLHelper.CurrentHeight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AbsToGL(float x, float y)
        {
            return new Vector2(RelXToGLX(AbsXToRelX(x)), RelYToGLY(AbsYToRelY(y)));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RelToGL(float x, float y)
        {
            return new Vector2(RelXToGLX(x), RelYToGLY(y));
        }

        public static void GetResourceBitmap(string resource, out Size2D size, out uint[] dstBmp)
        {
            Stream s = Utils.GetResourceStream(resource);
            var img = SixLabors.ImageSharp.Image.Load<Rgba32>(s);
            s.Dispose();
            size.Width = (uint)img.Width;
            size.Height = (uint)img.Height;
            dstBmp = new uint[size.Width * size.Height];
            fixed (uint* dst = dstBmp)
            {
                uint len = size.Width * size.Height * sizeof(uint);
                fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
                {
                    System.Buffer.MemoryCopy(data, dst, len, len);
                }
            }
            img.Dispose();
        }

        public static Image[] GetResourceSheetAsImages(string resource, Size2D imageSize)
        {
            uint[][] bitmaps = GetResourceSheetAsBitmaps(resource, imageSize);
            var arr = new Image[bitmaps.Length];
            for (int i = 0; i < bitmaps.Length; i++)
            {
                arr[i] = new Image(bitmaps[i], imageSize, resource + '[' + i + ']');
            }
            return arr;
        }
        public static uint[][] GetResourceSheetAsBitmaps(string resource, Size2D imageSize)
        {
            GetResourceBitmap(resource, out Size2D sheetSize, out uint[] srcBmp);
            fixed (uint* src = srcBmp)
            {
                uint numImagesX = sheetSize.Width / imageSize.Width;
                uint numImagesY = sheetSize.Height / imageSize.Height;
                uint[][] imgs = new uint[numImagesX * numImagesY][];
                int img = 0;
                for (uint sy = 0; sy < numImagesY; sy++)
                {
                    for (uint sx = 0; sx < numImagesX; sx++)
                    {
                        imgs[img++] = GetBitmap_Unchecked(src, sheetSize.Width, new Pos2D((int)(sx * imageSize.Width), (int)(sy * imageSize.Height)), imageSize);
                    }
                }
                return imgs;
            }
        }

        #endregion

        #region Utilities

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCoordinatesForCentering(uint dstSize, uint srcSize, float pos)
        {
            return (int)((uint)(dstSize * pos) - (srcSize / 2));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCoordinatesForEndAlign(uint dstSize, uint srcSize, float pos)
        {
            return (int)((uint)(dstSize * pos) - srcSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetAnimationProgress(TimeSpan end, ref TimeSpan cur)
        {
            cur += Game.RenderTimeSinceLastFrame;
            return Utils.GetProgress(end, cur);
        }

        #endregion
    }
}
