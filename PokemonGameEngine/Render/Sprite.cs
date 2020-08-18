using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class Sprite : ISprite
    {
        public uint[] Bitmap { get; set; }
        public int Width { get; }
        public int Height { get; }

        private Sprite(string resource)
        {
            using (Stream stream = Utils.GetResourceStream(resource))
            {
                RenderUtils.GetTextureData(stream, out int width, out int height, out uint[] bitmap);
                Bitmap = bitmap;
                Width = width;
                Height = height;
            }
        }
        public Sprite(int width, int height)
        {
            Bitmap = new uint[width * height];
            Width = width;
            Height = height;
        }
        public Sprite(uint[] bitmap, int spriteWidth, int spriteHeight)
        {
            Bitmap = bitmap;
            Width = spriteWidth;
            Height = spriteHeight;
        }

        private static readonly Dictionary<string, WeakReference<Sprite>> _loadedSprites = new Dictionary<string, WeakReference<Sprite>>();
        public static Sprite LoadOrGet(string resource)
        {
            Sprite s;
            if (!_loadedSprites.TryGetValue(resource, out WeakReference<Sprite> w))
            {
                s = new Sprite(resource);
                _loadedSprites.Add(resource, new WeakReference<Sprite>(s));
            }
            else if (!w.TryGetTarget(out s))
            {
                s = new Sprite(resource);
                w.SetTarget(s);
            }
            return s;
        }
    }
}
