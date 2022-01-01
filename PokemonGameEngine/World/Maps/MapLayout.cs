using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.World;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.World.Maps
{
    /// <summary>MapLayouts are not cached because they can individually be changed during the game</summary>
    internal sealed class MapLayout
    {
        public sealed class Block
        {
            public readonly byte Elevations;
            public readonly LayoutBlockPassage Passage;

            /// <summary>Also unused for now but can help when updating _usedBlocksets when changing Blocks</summary>
            private readonly Blockset _blockset;
            public readonly Blockset.Block BlocksetBlock;

            public Block(EndianBinaryReader r, List<Blockset> used)
            {
                Elevations = r.ReadByte();
                Passage = r.ReadEnum<LayoutBlockPassage>();
                int blocksetId = r.ReadInt32();
                int blockId = r.ReadInt32();

                // Load blockset if it's not loaded already by this layout
                _blockset = used.Find(b => b.Id == blocksetId);
                if (_blockset is null)
                {
                    _blockset = Blockset.LoadOrGet(blocksetId);
                    used.Add(_blockset);
                }
                BlocksetBlock = _blockset.Blocks[blockId];
            }
        }

        public readonly int BlocksWidth;
        public readonly int BlocksHeight;
        public readonly Block[][] Blocks;
        public readonly byte BorderWidth;
        public readonly byte BorderHeight;
        public readonly Block[][] BorderBlocks;

        /// <summary>Keeps track of which tilesets are loaded so they can be unloaded later.</summary>
        private readonly List<Blockset> _usedBlocksets;

        public MapLayout(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            using (var r = new EndianBinaryReader(AssetLoader.GetAssetStream(LayoutPath + name + LayoutExtension)))
            {
                // Layout blocks
                BlocksWidth = r.ReadInt32();
                if (BlocksWidth <= 0)
                {
                    throw new InvalidDataException();
                }
                BlocksHeight = r.ReadInt32();
                if (BlocksHeight <= 0)
                {
                    throw new InvalidDataException();
                }
                _usedBlocksets = new List<Blockset>();
                Blocks = new Block[BlocksHeight][];
                for (int y = 0; y < BlocksHeight; y++)
                {
                    var arrY = new Block[BlocksWidth];
                    for (int x = 0; x < BlocksWidth; x++)
                    {
                        arrY[x] = new Block(r, _usedBlocksets);
                    }
                    Blocks[y] = arrY;
                }

                // Border blocks
                BorderWidth = r.ReadByte();
                BorderHeight = r.ReadByte();
                if (BorderWidth == 0 || BorderHeight == 0)
                {
                    BorderBlocks = Array.Empty<Block[]>();
                }
                else
                {
                    BorderBlocks = new Block[BorderHeight][];
                    for (int y = 0; y < BorderHeight; y++)
                    {
                        var arrY = new Block[BorderWidth];
                        for (int x = 0; x < BorderWidth; x++)
                        {
                            arrY[x] = new Block(r, _usedBlocksets);
                        }
                        BorderBlocks[y] = arrY;
                    }
                }
            }
        }

        public Block GetBlock_InBounds(Pos2D xy)
        {
            bool north = xy.Y < 0;
            bool south = xy.Y >= BlocksHeight;
            bool west = xy.X < 0;
            bool east = xy.X >= BlocksWidth;
            // In bounds
            if (!north && !south && !west && !east)
            {
                return Blocks[xy.Y][xy.X];
            }
            // Border blocks
            byte bw = BorderWidth;
            byte bh = BorderHeight;
            // No border should render pure black
            if (bw == 0 || bh == 0)
            {
                return null;
            }
            // Has a border
            xy.X %= bw;
            if (west)
            {
                xy.X *= -1;
            }
            xy.Y %= bh;
            if (north)
            {
                xy.Y *= -1;
            }
            return BorderBlocks[xy.Y][xy.X];
        }

        public void Delete()
        {
            foreach (Blockset b in _usedBlocksets)
            {
                b.DeductReference();
            }
        }

        #region Loading

        private const string LayoutExtension = ".pgelayout";
        private const string LayoutPath = "Layout\\";
        private static readonly IdList _ids = new(LayoutPath + "LayoutIds.txt");

        #endregion
    }
}
