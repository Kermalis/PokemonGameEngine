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
            public readonly Blockset.Block BlocksetBlock;

            public Block(EndianBinaryReader r, List<Blockset> used)
            {
                Elevations = r.ReadByte();
                Passage = r.ReadEnum<LayoutBlockPassage>();
                int blocksetId = r.ReadInt32();
                int blockId = r.ReadInt32();

                // Load blockset if it's not loaded already by this layout
                Blockset blockset = used.Find(b => b.Id == blocksetId);
                if (blockset is null)
                {
                    blockset = Blockset.LoadOrGet(blocksetId);
                    used.Add(blockset);
                }
                BlocksetBlock = blockset.Blocks[blockId];
            }
        }

        private const string LAYOUT_PATH = @"Layout\";
        private const string LAYOUT_EXTENSION = ".pgelayout";
        private static readonly IdList _ids = new(LAYOUT_PATH + "LayoutIds.txt");

#if DEBUG_OVERWORLD
        public readonly string Name;
#endif
        public readonly Vec2I Size;
        public readonly Block[][] Blocks;
        public readonly Vec2I BorderSize;
        public readonly Block[][] BorderBlocks;

        /// <summary>Keeps track of which tilesets are loaded so they can be unloaded later</summary>
        private readonly List<Blockset> _usedBlocksets;
        /// <summary>Keeps track of which blocks are being used by the layout so we can manage their textures when they become used/unused</summary>
        private readonly List<Blockset.Block> _usedBlocks;

        public MapLayout(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
#if DEBUG_OVERWORLD
            Name = name;
#endif
            using (var r = new EndianBinaryReader(AssetLoader.GetAssetStream(LAYOUT_PATH + name + LAYOUT_EXTENSION)))
            {
                // Layout blocks
                Size.X = r.ReadInt32();
                if (Size.X <= 0)
                {
                    throw new InvalidDataException();
                }
                Size.Y = r.ReadInt32();
                if (Size.Y <= 0)
                {
                    throw new InvalidDataException();
                }

                _usedBlocksets = new List<Blockset>();
                _usedBlocks = new List<Blockset.Block>();
                Blocks = CreateBlocks(r, Size);

                // Border blocks
                BorderSize.X = r.ReadByte();
                BorderSize.Y = r.ReadByte();
                if (BorderSize.X == 0 || BorderSize.Y == 0)
                {
                    BorderBlocks = Array.Empty<Block[]>();
                }
                else
                {
                    BorderBlocks = CreateBlocks(r, BorderSize);
                }

                if (_usedBlocks.Count > 0)
                {
                    Blockset.EvaluateUsedBlocks(_usedBlocks);
                }
            }
        }
        private Block[][] CreateBlocks(EndianBinaryReader r, Vec2I size)
        {
            var ret = new Block[size.Y][];
            for (int y = 0; y < size.Y; y++)
            {
                var arrY = new Block[size.X];
                for (int x = 0; x < size.X; x++)
                {
                    arrY[x] = new Block(r, _usedBlocksets);
                    Blockset.Block b = arrY[x].BlocksetBlock;
                    if (!_usedBlocks.Contains(b))
                    {
                        _usedBlocks.Add(b);
                        b.AddReference(this);
                    }
                }
                ret[y] = arrY;
            }
            return ret;
        }

        public Vec2I GetBorderBlockIndex(Vec2I pos)
        {
            pos %= BorderSize;
            if (pos.X < 0)
            {
                pos.X += BorderSize.X;
            }
            if (pos.Y < 0)
            {
                pos.Y += BorderSize.Y;
            }
            return pos;
        }
        public Block GetBlock(Vec2I xy)
        {
            bool north = xy.Y < 0;
            bool south = xy.Y >= Size.Y;
            bool west = xy.X < 0;
            bool east = xy.X >= Size.X;
            // In bounds
            if (!north && !south && !west && !east)
            {
                return Blocks[xy.Y][xy.X];
            }

            // Border blocks
            if (BorderSize.X == 0 || BorderSize.Y == 0)
            {
                return null; // No border
            }
            xy = GetBorderBlockIndex(xy);
            return BorderBlocks[xy.Y][xy.X];
        }

        public void Delete()
        {
            foreach (Blockset.Block b in _usedBlocks)
            {
                b.DeductReference(this);
            }
            foreach (Blockset b in _usedBlocksets)
            {
                b.DeductReference();
            }
        }

#if DEBUG_OVERWORLD
        public override string ToString()
        {
            return Name;
        }
#endif
    }
}
