﻿using Kermalis.PokemonGameEngine.Render;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World
{
    internal sealed class Tileset
    {
        public sealed class Tile
        {
            public readonly Tileset Parent;
            public readonly uint[] Bitmap;

            public Tile(Tileset parent, uint[] bitmap)
            {
                Parent = parent;
                Bitmap = bitmap;
            }
        }

        public readonly Tile[] Tiles;

        private Tileset(string name)
        {
            uint[][] t = RenderUtils.LoadBitmapSheet(_tilesetPath + name + _tilesetExtension, Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY);
            Tiles = new Tile[t.Length];
            for (int i = 0; i < t.Length; i++)
            {
                Tiles[i] = new Tile(this, t[i]);
            }
        }

        private const string _tilesetExtension = ".png";
        private const string _tilesetPath = "Tileset.";
        private static readonly IdList _ids = new IdList(_tilesetPath + "TilesetIds.txt");
        private static readonly Dictionary<int, WeakReference<Tileset>> _loadedTilesets = new Dictionary<int, WeakReference<Tileset>>();
        public static Tileset LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            Tileset t;
            if (!_loadedTilesets.ContainsKey(id))
            {
                t = new Tileset(name);
                _loadedTilesets.Add(id, new WeakReference<Tileset>(t));
                return t;
            }
            if (_loadedTilesets[id].TryGetTarget(out t))
            {
                return t;
            }
            t = new Tileset(name);
            _loadedTilesets[id].SetTarget(t);
            return t;
        }
    }
}