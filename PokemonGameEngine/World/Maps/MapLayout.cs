using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.World;
using System;
using System.IO;

namespace Kermalis.PokemonGameEngine.World.Maps
{
    /// <summary>MapLayouts are not cached because they can individually be changed during the game</summary>
    internal sealed class MapLayout
    {
        public sealed class Block
        {
            /// <summary>Currently unused, but may be needed when we are changing Blocks and need to update stuff</summary>
            public readonly MapLayout Parent;

            public readonly byte Elevations;
            public readonly LayoutBlockPassage Passage;
            public readonly Blockset.Block BlocksetBlock;

            public Block(MapLayout parent, EndianBinaryReader r)
            {
                Parent = parent;

                Elevations = r.ReadByte();
                Passage = r.ReadEnum<LayoutBlockPassage>();
                BlocksetBlock = Blockset.LoadOrGet(r.ReadInt32()).Blocks[r.ReadInt32()];
            }
        }

        public readonly int BlocksWidth;
        public readonly int BlocksHeight;
        public readonly Block[][] Blocks;
        public readonly byte BorderWidth;
        public readonly byte BorderHeight;
        public readonly Block[][] BorderBlocks;

        public MapLayout(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            using (var r = new EndianBinaryReader(Utils.GetResourceStream(LayoutPath + name + LayoutExtension)))
            {
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
                Blocks = new Block[BlocksHeight][];
                for (int y = 0; y < BlocksHeight; y++)
                {
                    var arrY = new Block[BlocksWidth];
                    for (int x = 0; x < BlocksWidth; x++)
                    {
                        arrY[x] = new Block(this, r);
                    }
                    Blocks[y] = arrY;
                }
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
                            arrY[x] = new Block(this, r);
                        }
                        BorderBlocks[y] = arrY;
                    }
                }
            }
        }

        #region Loading

        private const string LayoutExtension = ".pgelayout";
        private const string LayoutPath = "Layout.";
        private static readonly IdList _ids = new(LayoutPath + "LayoutIds.txt");

        #endregion
    }
}
