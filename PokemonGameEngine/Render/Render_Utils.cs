using Kermalis.PokemonGameEngine.Util;
using SDL2;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Render
{
    // https://stackoverflow.com/questions/24771828/algorithm-for-creating-rounded-corners-in-a-polygon
    // ^^ This could be cool, not sure if we'd need it yet though

    internal static unsafe partial class Renderer
    {
        #region SDL

        private static IntPtr ConvertSurfaceFormat(IntPtr surface)
        {
            IntPtr result = surface;
            var surPtr = (SDL.SDL_Surface*)surface;
            var pixelFormatPtr = (SDL.SDL_PixelFormat*)surPtr->format;
            if (pixelFormatPtr->format != SDL.SDL_PIXELFORMAT_ABGR8888)
            {
                result = SDL.SDL_ConvertSurfaceFormat(surface, SDL.SDL_PIXELFORMAT_ABGR8888, 0);
                SDL.SDL_FreeSurface(surface);
            }
            return result;
        }
        private static IntPtr GetSurfacePixels(IntPtr surface)
        {
            return ((SDL.SDL_Surface*)surface)->pixels;
        }
        private static int GetSurfaceWidth(IntPtr surface)
        {
            return ((SDL.SDL_Surface*)surface)->w;
        }
        private static int GetSurfaceHeight(IntPtr surface)
        {
            return ((SDL.SDL_Surface*)surface)->h;
        }
        private static IntPtr ResourceToRWops(string resource)
        {
            byte[] bytes;
            using (Stream stream = Utils.GetResourceStream(resource))
            {
                bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
            }
            fixed (byte* src = bytes)
            {
                return SDL.SDL_RWFromMem(new IntPtr(src), bytes.Length);
            }
        }
        public static void GetResourceBitmap(string resource, out int width, out int height, out uint[] dstBmp)
        {
            IntPtr rwops = ResourceToRWops(resource);
            IntPtr surface = SDL_image.IMG_Load_RW(rwops, 1);
            surface = ConvertSurfaceFormat(surface);
            width = GetSurfaceWidth(surface);
            height = GetSurfaceHeight(surface);
            dstBmp = new uint[width * height];
            fixed (uint* dst = dstBmp)
            {
                int len = width * height * sizeof(uint);
                Buffer.MemoryCopy(GetSurfacePixels(surface).ToPointer(), dst, len, len);
            }
            SDL.SDL_FreeSurface(surface);
            SDL.SDL_FreeRW(rwops);
        }

        #endregion

        #region Sheets

        public static Image[] GetResourceSheetAsImages(string resource, int imageWidth, int imageHeight)
        {
            uint[][] bitmaps = GetResourceSheetAsBitmaps(resource, imageWidth, imageHeight);
            var arr = new Image[bitmaps.Length];
            for (int i = 0; i < bitmaps.Length; i++)
            {
                arr[i] = new Image(bitmaps[i], imageWidth, imageHeight);
            }
            return arr;
        }
        public static uint[][] GetResourceSheetAsBitmaps(string resource, int imageWidth, int imageHeight)
        {
            GetResourceBitmap(resource, out int sheetWidth, out int sheetHeight, out uint[] srcBmp);
            fixed (uint* src = srcBmp)
            {
                int numImagesX = sheetWidth / imageWidth;
                int numImagesY = sheetHeight / imageHeight;
                uint[][] imgs = new uint[numImagesX * numImagesY][];
                int img = 0;
                for (int sy = 0; sy < numImagesY; sy++)
                {
                    for (int sx = 0; sx < numImagesX; sx++)
                    {
                        imgs[img++] = GetBitmap_Unchecked(src, sheetWidth, sx * imageWidth, sy * imageHeight, imageWidth, imageHeight);
                    }
                }
                return imgs;
            }
        }

        #endregion

        #region Utilities

        /// <summary>Returns true if any pixel is inside of the target bitmap.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInsideBitmap(int bmpWidth, int bmpHeight, int x, int y, int w, int h)
        {
            return x < bmpWidth && x + w > 0 && y < bmpHeight && y + h > 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCoordinatesForCentering(int dstSize, int srcSize, float pos)
        {
            return (int)(dstSize * pos) - (srcSize / 2);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCoordinatesForEndAlign(int dstSize, int srcSize, float pos)
        {
            return (int)(dstSize * pos) - srcSize;
        }

        #endregion
    }
}
