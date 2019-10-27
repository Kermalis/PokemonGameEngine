using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.EndianBinaryIO;
using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    internal sealed class Map : IDisposable
    {
        public sealed class Block
        {
            public readonly int X;
            public readonly int Y;

            public byte Behavior;
            public Blockset.Block BlocksetBlock;

            public Block(int x, int y, EndianBinaryReader r)
            {
                X = x;
                Y = y;
                Behavior = r.ReadByte();
                BlocksetBlock = Blockset.LoadOrGet(r.ReadInt32()).Blocks[r.ReadInt32()];
            }
            public Block(int x, int y, Blockset.Block defaultBlock)
            {
                X = x;
                Y = y;
                BlocksetBlock = defaultBlock;
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(Behavior);
                w.Write(BlocksetBlock.Parent.Id);
                w.Write(BlocksetBlock.Id);
            }
        }

        public WriteableBitmap Bitmap;
        public event EventHandler<EventArgs> OnDrew;

        public int Width;
        public int Height;

        public Block[][] Blocks;

        public Map(string name)
        {
            using (var r = new EndianBinaryReader(File.OpenRead(Path.Combine(Program.AssetPath, "Map", name + ".pgemap"))))
            {
                Width = r.ReadInt32();
                Height = r.ReadInt32();
                Blocks = new Block[Height][];
                for (int y = 0; y < Height; y++)
                {
                    var arrY = new Block[Width];
                    for (int x = 0; x < Width; x++)
                    {
                        arrY[x] = new Block(x, y, r);
                    }
                    Blocks[y] = arrY;
                }
                UpdateBitmapSize();
            }
        }
        public Map(int width, int height, Blockset.Block defaultBlock)
        {
            Width = width;
            Height = height;
            Blocks = new Block[Height][];
            for (int y = 0; y < Height; y++)
            {
                var arrY = new Block[Width];
                for (int x = 0; x < Width; x++)
                {
                    arrY[x] = new Block(x, y, defaultBlock);
                }
                Blocks[y] = arrY;
            }
            UpdateBitmapSize();
        }
        ~Map()
        {
            Dispose(false);
        }

        public void Paste(Blockset.Block[][] blocks, int destX, int destY)
        {
            for (int y = 0; y < blocks.Length; y++)
            {
                int dy = y + destY;
                if (dy >= 0 && dy < Height)
                {
                    Blockset.Block[] inArrY = blocks[y];
                    Block[] outArrY = Blocks[dy];
                    for (int x = 0; x < inArrY.Length; x++)
                    {
                        int dx = x + destX;
                        if (dx >= 0 && dx < Width)
                        {
                            Blockset.Block b = inArrY[x];
                            if (b != null)
                            {
                                Block outB = outArrY[dx];
                                if (outB.BlocksetBlock != b)
                                {
                                    outB.BlocksetBlock = b;
                                    DrawList.Add(outB);
                                }
                            }
                        }
                    }
                }
            }
            Draw();
        }

        private void UpdateBitmapSize()
        {
            bool createNew;
            if (Bitmap == null)
            {
                createNew = true;
            }
            else
            {
                PixelSize ps = Bitmap.PixelSize;
                createNew = ps.Width != Width * 16 || ps.Height != Height * 16;
            }
            if (createNew)
            {
                Bitmap?.Dispose();
                Bitmap = new WriteableBitmap(new PixelSize(Width * 16, Height * 16), new Vector(96, 96), PixelFormat.Bgra8888);
                DrawAll();
            }
        }
        public static readonly List<Block> DrawList = new List<Block>(); // Save allocations
        public unsafe void Draw()
        {
            if (DrawList.Count > 0)
            {
                using (ILockedFramebuffer l = Bitmap.Lock())
                {
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    int bmpWidth = Width * 16;
                    int bmpHeight = Height * 16;
                    for (int i = 0; i < DrawList.Count; i++)
                    {
                        Block b = DrawList[i];
                        int x = b.X * 16;
                        int y = b.Y * 16;
                        RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, x, y, 16, 16, 0xFF000000);
                        b.BlocksetBlock.Draw(bmpAddress, bmpWidth, bmpHeight, x, y);
                    }
                }
                DrawList.Clear();
                OnDrew?.Invoke(this, EventArgs.Empty);
            }
        }
        public unsafe void DrawAll()
        {
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                int bmpWidth = Width * 16;
                int bmpHeight = Height * 16;
                RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, 0xFF000000);
                for (int y = 0; y < Height; y++)
                {
                    Block[] arrY = Blocks[y];
                    for (int x = 0; x < Width; x++)
                    {
                        arrY[x].BlocksetBlock.Draw(bmpAddress, bmpWidth, bmpHeight, x * 16, y * 16);
                    }
                }
            }
            OnDrew?.Invoke(this, EventArgs.Empty);
        }

        public void Save(string name)
        {
            using (var w = new EndianBinaryWriter(File.Create(Path.Combine(Program.AssetPath, "Map", name + ".pgemap"))))
            {
                w.Write(Width);
                w.Write(Height);
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Blocks[y][x].Write(w);
                    }
                }
            }
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
