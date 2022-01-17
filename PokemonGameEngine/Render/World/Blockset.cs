using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders.World;
using Kermalis.PokemonGameEngine.World;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Debug;
#endif

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed partial class Blockset
    {
        private const string BLOCKSET_PATH = @"Blockset\";
        private const string BLOCKSET_EXTENSION = ".pgeblockset";
        private const int DEFAULT_USED_BLOCKS_CAPACITY = 256; // Will expand itself if it needs more
        private const int USED_BLOCK_INDEX_NONE = -1;

        private static readonly IdList _ids = new(BLOCKSET_PATH + "BlocksetIds.txt");
        private static readonly Dictionary<int, Blockset> _loadedBlocksets = new();
        public static FrameBuffer3DColor[] UsedBlocksTextures;
        private static List<Block> _usedBlocks;

#if DEBUG_OVERWORLD
        public readonly string Name;
#endif
        public readonly int Id;
        private int _numReferences;

        public readonly Block[] Blocks;
        private readonly List<Block> _animatedBlocks;
        /// <summary>Keeps track of which tilesets are loaded so they can be unloaded later.</summary>
        private readonly List<Tileset> _usedTilesets;

        private Blockset(int id, string name)
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Loading blockset: " + name);
            Log.ModifyIndent(+1);
            Name = name;
#endif

            Id = id;
            _numReferences = 1;
            _loadedBlocksets.Add(id, this);

            using (var r = new EndianBinaryReader(File.OpenRead(AssetLoader.GetPath(BLOCKSET_PATH + name + BLOCKSET_EXTENSION))))
            {
                ushort count = r.ReadUInt16();
                if (count == 0)
                {
                    throw new InvalidDataException("Empty blockset: " + name);
                }

                _animatedBlocks = new List<Block>();
                _usedTilesets = new List<Tileset>();
                Blocks = new Block[count];
                for (int i = 0; i < count; i++)
                {
                    Blocks[i] = new Block(r, this);
                }
            }

#if DEBUG_OVERWORLD
            Log.ModifyIndent(-1);
#endif
        }

        public static void Init()
        {
            UsedBlocksTextures = new FrameBuffer3DColor[Overworld.NumElevations];
            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                UsedBlocksTextures[e] = new FrameBuffer3DColor(Overworld.Block_NumPixels, DEFAULT_USED_BLOCKS_CAPACITY);
            }
            _usedBlocks = new List<Block>(DEFAULT_USED_BLOCKS_CAPACITY);
        }
        public static void EvaluateUsedBlocks(List<Block> usedLayoutBlocks)
        {
            // Check if we need to resize the textures
            bool resized = false;
            uint num = UsedBlocksTextures[0].NumLayers;
            int curNum = _usedBlocks.Count;
            while (curNum > num)
            {
                num *= 2;
                resized = true;
            }

            // Prepare to draw some blocks
            GL gl = Display.OpenGL;
            BlocksetBlockShader.Instance.Use(gl);
            gl.ClearColor(Colors.Transparent);
            var builder = new TileVertexBuilder();
            Blockset blockset = null; // Cache so we don't keep changing texture units for no reason

            if (resized)
            {
                // Resize textures
                for (byte e = 0; e < Overworld.NumElevations; e++)
                {
                    FrameBuffer3DColor fb = UsedBlocksTextures[e];
                    fb.Use(gl);
                    fb.UpdateTexture(num);
                }
                // When we resize, we want to draw all used blocks to the new FBOs
                for (int i = 0; i < curNum; i++)
                {
                    Block b = _usedBlocks[i]; // If a resize was triggered, that means all indices are not null
                    if (b.Parent != blockset)
                    {
                        blockset = b.Parent;
                        blockset.BindTilesetTextures();
                    }
                    DrawBlock(gl, builder, b);
                }
            }
            else
            {
                // If we didn't resize, just draw the new blocks
                for (int i = 0; i < usedLayoutBlocks.Count; i++)
                {
                    Block b = usedLayoutBlocks[i];
                    if (b.LayoutsUsing.Count != 1)
                    {
                        continue; // This block is not new
                    }
                    if (b.Parent != blockset)
                    {
                        blockset = b.Parent;
                        blockset.BindTilesetTextures();
                    }
                    DrawBlock(gl, builder, b);
                }
            }
        }
        private static void DrawBlock(GL gl, TileVertexBuilder builder, Block b)
        {
            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                FrameBuffer3DColor fb = UsedBlocksTextures[e];
                fb.Use(gl);
                fb.SetLayer(b.UsedBlocksIndex);
                b.Draw(builder, e);
            }
        }

        private void BindTilesetTextures()
        {
            GL gl = Display.OpenGL;
            for (int i = 0; i < _usedTilesets.Count; i++)
            {
                gl.ActiveTexture(i.ToTextureUnit());
                gl.BindTexture(TextureTarget.Texture2D, _usedTilesets[i].Texture);
            }
        }

        public static void UpdateAnimations()
        {
            if (_loadedBlocksets.Count == 0)
            {
                return;
            }

            TileVertexBuilder builder = null;
            foreach (Blockset blockset in _loadedBlocksets.Values)
            {
                int num = blockset._animatedBlocks.Count; // Only used blocks would be in this list
                if (num == 0)
                {
                    continue;
                }

                for (int i = 0; i < num; i++)
                {
                    Block b = blockset._animatedBlocks[i];
                    if (b.IsAnimDirty())
                    {
                        GL gl = Display.OpenGL;
                        // Init on first one
                        if (builder is null)
                        {
                            builder = new TileVertexBuilder();
                            BlocksetBlockShader.Instance.Use(gl);
                            gl.ClearColor(Colors.Transparent);
                            blockset.BindTilesetTextures();
                        }
                        DrawBlock(gl, builder, b);
                    }
                }
            }
        }

        public static Blockset LoadOrGet(int id)
        {
            if (_loadedBlocksets.TryGetValue(id, out Blockset b))
            {
                b._numReferences++;
#if DEBUG_OVERWORLD
                Log.WriteLine("Adding reference to blockset: " + b.Name + " (new count is " + b._numReferences + ")");
#endif
            }
            else
            {
                string name = _ids[id];
                if (name is null)
                {
                    throw new ArgumentOutOfRangeException(nameof(id));
                }
                b = new Blockset(id, name);
            }
            return b;
        }
        public void DeductReference()
        {
            if (--_numReferences > 0)
            {
#if DEBUG_OVERWORLD
                Log.WriteLine("Removing reference from blockset: " + _ids[Id] + " (new count is " + _numReferences + ")");
#endif
                return;
            }

#if DEBUG_OVERWORLD
            Log.WriteLine("Unloading blockset: " + _ids[Id]);
            Log.ModifyIndent(+1);
#endif
            foreach (Tileset t in _usedTilesets)
            {
                t.DeductReference();
            }
            _loadedBlocksets.Remove(Id);
#if DEBUG_OVERWORLD
            Log.ModifyIndent(-1);
#endif
        }

#if DEBUG_OVERWORLD
        public override string ToString()
        {
            return Name;
        }
#endif
    }
}
