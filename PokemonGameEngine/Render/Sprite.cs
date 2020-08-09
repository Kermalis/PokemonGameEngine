using Avalonia.Media.Imaging;
using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class Sprite
    {
        public uint[] Bitmap { get; }
        public int Width { get; }
        public int Height { get; }

        private Sprite(string resource)
        {
            using (var wb = RenderUtils.ToWriteableBitmap(new Bitmap(Utils.GetResourceStream(resource))))
            {
                Bitmap = RenderUtils.ToBitmap(wb, out int w, out int h);
                Width = w;
                Height = h;
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
            if (!_loadedSprites.ContainsKey(resource))
            {
                s = new Sprite(resource);
                _loadedSprites.Add(resource, new WeakReference<Sprite>(s));
                return s;
            }
            WeakReference<Sprite> w = _loadedSprites[resource];
            if (w.TryGetTarget(out s))
            {
                return s;
            }
            s = new Sprite(resource);
            w.SetTarget(s);
            return s;
        }

        public unsafe delegate void DrawMethod(uint* bmpAddress, int bmpWidth, int bmpHeight);
        public unsafe void Draw(DrawMethod drawMethod)
        {
            fixed (uint* bmpAddress = Bitmap)
            {
                drawMethod(bmpAddress, Width, Height);
            }
        }

        public unsafe void DrawOn(Sprite otherSprite, int x, int y)
        {
            fixed (uint* otherBmpAddress = otherSprite.Bitmap)
            {
                DrawOn(otherBmpAddress, otherSprite.Width, otherSprite.Height, x, y);
            }
        }
        public unsafe void DrawOn(uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, int x, int y)
        {
            fixed (uint* bmpAddress = Bitmap)
            {
                RenderUtils.DrawBitmap(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, bmpAddress, Width, Height);
            }
        }
        public unsafe void DrawOn(Sprite otherSprite, int x, int y, int width, int height)
        {
            fixed (uint* otherBmpAddress = otherSprite.Bitmap)
            {
                DrawOn(otherBmpAddress, otherSprite.Width, otherSprite.Height, x, y, width, height);
            }
        }
        public unsafe void DrawOn(uint* otherBmpAddress, int otherBmpWidth, int otherBmpHeight, int x, int y, int width, int height)
        {
            fixed (uint* bmpAddress = Bitmap)
            {
                RenderUtils.DrawBitmap(otherBmpAddress, otherBmpWidth, otherBmpHeight, x, y, width, height, bmpAddress, Width, Height);
            }
        }
    }
}
