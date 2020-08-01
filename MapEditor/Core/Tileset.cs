using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    public sealed class Tileset : IDisposable
    {
        public sealed class Tile
        {
            public readonly Tileset Parent;
            public readonly int Id;
            public readonly uint[] Bitmap;

            public Tile(Tileset parent, int id, uint[] bitmap)
            {
                Parent = parent;
                Id = id;
                Bitmap = bitmap;
            }
        }

        internal readonly int BitmapNumTilesX;
        internal readonly WriteableBitmap Bitmap;

        internal readonly string Name;
        internal readonly int Id;
        internal readonly Tile[] Tiles;

        private unsafe Tileset(string name, int id)
        {
            uint[][] t = RenderUtils.LoadBitmapSheet(Path.Combine(_tilesetPath, name + _tilesetExtension), 8, 8, out int bmpWidth, out int bmpHeight);
            BitmapNumTilesX = bmpWidth / 8;
            Tiles = new Tile[t.Length];
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tiles[i] = new Tile(this, i, t[i]);
            }
            Name = name;
            Id = id;
            // Draw
            Bitmap = new WriteableBitmap(new PixelSize(bmpWidth, bmpHeight), new Vector(96, 96), PixelFormat.Bgra8888);
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtils.TransparencyGrid(bmpAddress, bmpWidth, bmpHeight, 4, 4);
                int x = 0;
                int y = 0;
                for (int i = 0; i < Tiles.Length; i++, x++)
                {
                    if (x >= BitmapNumTilesX)
                    {
                        x = 0;
                        y++;
                    }
                    RenderUtils.DrawBitmap(bmpAddress, bmpWidth, bmpHeight, x * 8, y * 8, Tiles[i].Bitmap, 8, 8, xFlip: false, yFlip: false);
                }
                for (; x < BitmapNumTilesX; x++)
                {
                    RenderUtils.DrawCrossUnchecked(bmpAddress, bmpWidth, x * 8, y * 8, 8, 8, 0xFFFF0000);
                }
            }
        }
        ~Tileset()
        {
            Dispose(false);
        }

        private const string _tilesetExtension = ".png";
        private static readonly string _tilesetPath = Path.Combine(Program.AssetPath, "Tileset");
        public static IdList Ids { get; } = new IdList(Path.Combine(_tilesetPath, "TilesetIds.txt"));
        private static readonly Dictionary<int, WeakReference<Tileset>> _loadedTilesets = new Dictionary<int, WeakReference<Tileset>>();
        internal static Tileset LoadOrGet(string name)
        {
            int id = Ids[name];
            if (id == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            return LoadOrGet(name, id);
        }
        internal static Tileset LoadOrGet(int id)
        {
            string name = Ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            return LoadOrGet(name, id);
        }
        private static Tileset LoadOrGet(string name, int id)
        {
            Tileset t;
            if (!_loadedTilesets.ContainsKey(id))
            {
                t = new Tileset(name, id);
                _loadedTilesets.Add(id, new WeakReference<Tileset>(t));
                return t;
            }
            if (_loadedTilesets[id].TryGetTarget(out t))
            {
                return t;
            }
            t = new Tileset(name, id);
            _loadedTilesets[id].SetTarget(t);
            return t;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
            Bitmap.Dispose();
        }
    }
}
