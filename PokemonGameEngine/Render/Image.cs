using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class Image : IImage
    {
        public uint[] Bitmap { get; }
        public int Width { get; }
        public int Height { get; }

        private Image(string resource)
        {
            RenderUtils.GetTextureData(resource, out int width, out int height, out uint[] bitmap);
            Bitmap = bitmap;
            Width = width;
            Height = height;
        }
        public Image(int width, int height)
        {
            Bitmap = new uint[width * height];
            Width = width;
            Height = height;
        }
        public Image(uint[] bitmap, int width, int height)
        {
            Bitmap = bitmap;
            Width = width;
            Height = height;
        }

        private static readonly Dictionary<string, WeakReference<Image>> _loadedImages = new Dictionary<string, WeakReference<Image>>();
        public static Image LoadOrGet(string resource)
        {
            Image i;
            if (!_loadedImages.TryGetValue(resource, out WeakReference<Image> w))
            {
                i = new Image(resource);
                _loadedImages.Add(resource, new WeakReference<Image>(i));
            }
            else if (!w.TryGetTarget(out i))
            {
                i = new Image(resource);
                w.SetTarget(i);
            }
            return i;
        }
    }
}
