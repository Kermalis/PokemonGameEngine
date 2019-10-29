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

            public Block(bool isBorderBlock, int x, int y, EndianBinaryReader r)
            {
                X = x;
                Y = y;
                if (!isBorderBlock)
                {
                    Behavior = r.ReadByte();
                }
                BlocksetBlock = Blockset.LoadOrGet(r.ReadInt32()).Blocks[r.ReadInt32()];
            }
            public Block(int x, int y, Blockset.Block defaultBlock)
            {
                X = x;
                Y = y;
                BlocksetBlock = defaultBlock;
            }

            public void Write(bool isBorderBlock, EndianBinaryWriter w)
            {
                if (!isBorderBlock)
                {
                    w.Write(Behavior);
                }
                w.Write(BlocksetBlock.Parent.Id);
                w.Write(BlocksetBlock.Id);
            }
        }

        public WriteableBitmap BlocksBitmap;
        public WriteableBitmap BorderBlocksBitmap;
        public delegate void MapDrewBitmapEventHandler(Map map, bool drewBorderBlocks);
        public event MapDrewBitmapEventHandler OnDrew;

        public int Width;
        public int Height;
        public byte BorderWidth;
        public byte BorderHeight;

        public Block[][] Blocks;
        public Block[][] BorderBlocks;

        public Map(string name)
        {
            using (var r = new EndianBinaryReader(File.OpenRead(Path.Combine(Program.AssetPath, "Map", name + ".pgemap"))))
            {
                Block[][] Create(bool borderBlocks, int w, int h)
                {
                    var arr = new Block[h][];
                    for (int y = 0; y < h; y++)
                    {
                        var arrY = new Block[w];
                        for (int x = 0; x < w; x++)
                        {
                            arrY[x] = new Block(borderBlocks, x, y, r);
                        }
                        arr[y] = arrY;
                    }
                    return arr;
                }
                Blocks = Create(false, Width = r.ReadInt32(), Height = r.ReadInt32());
                BorderBlocks = Create(true, BorderWidth = r.ReadByte(), BorderHeight = r.ReadByte());
                UpdateBitmapSize(false);
                UpdateBitmapSize(true);
            }
        }
        public Map(int width, int height, byte borderWidth, byte borderHeight, Blockset.Block defaultBlock)
        {
            Block[][] Create(int w, int h)
            {
                var arr = new Block[h][];
                for (int y = 0; y < h; y++)
                {
                    var arrY = new Block[w];
                    for (int x = 0; x < w; x++)
                    {
                        arrY[x] = new Block(x, y, defaultBlock);
                    }
                    arr[y] = arrY;
                }
                return arr;
            }
            Blocks = Create(Width = width, Height = height);
            BorderBlocks = Create(BorderWidth = borderWidth, BorderHeight = borderHeight);
            UpdateBitmapSize(false);
            UpdateBitmapSize(true);
        }
        ~Map()
        {
            Dispose(false);
        }

        public void Paste(bool borderBlocks, Blockset.Block[][] blocks, int destX, int destY)
        {
            Block[][] outArr = borderBlocks ? BorderBlocks : Blocks;
            int width = borderBlocks ? BorderWidth : Width;
            int height = borderBlocks ? BorderHeight : Height;
            for (int y = 0; y < blocks.Length; y++)
            {
                int dy = y + destY;
                if (dy >= 0 && dy < height)
                {
                    Blockset.Block[] inArrY = blocks[y];
                    Block[] outArrY = outArr[dy];
                    for (int x = 0; x < inArrY.Length; x++)
                    {
                        int dx = x + destX;
                        if (dx >= 0 && dx < width)
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
            Draw(borderBlocks);
        }

        private void UpdateBitmapSize(bool borderBlocks)
        {
            WriteableBitmap bmp = borderBlocks ? BorderBlocksBitmap : BlocksBitmap;
            int bmpWidth = (borderBlocks ? BorderWidth : Width) * 16;
            int bmpHeight = (borderBlocks ? BorderHeight : Height) * 16;
            bool createNew;
            if (bmp == null)
            {
                createNew = true;
            }
            else
            {
                PixelSize ps = bmp.PixelSize;
                createNew = ps.Width != bmpWidth || ps.Height != bmpHeight;
            }
            if (createNew)
            {
                bmp?.Dispose();
                bmp = new WriteableBitmap(new PixelSize(bmpWidth, bmpHeight), new Vector(96, 96), PixelFormat.Bgra8888);
                if (borderBlocks)
                {
                    BorderBlocksBitmap = bmp;
                }
                else
                {
                    BlocksBitmap = bmp;
                }
                DrawAll(borderBlocks);
            }
        }
        public static readonly List<Block> DrawList = new List<Block>(); // Save allocations
        public unsafe void Draw(bool borderBlocks)
        {
            if (DrawList.Count > 0)
            {
                WriteableBitmap bmp = borderBlocks ? BorderBlocksBitmap : BlocksBitmap;
                using (ILockedFramebuffer l = bmp.Lock())
                {
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    int bmpWidth = (borderBlocks ? BorderWidth : Width) * 16;
                    int bmpHeight = (borderBlocks ? BorderHeight : Height) * 16;
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
                OnDrew?.Invoke(this, borderBlocks);
            }
        }
        public unsafe void DrawAll(bool borderBlocks)
        {
            WriteableBitmap bmp = borderBlocks ? BorderBlocksBitmap : BlocksBitmap;
            using (ILockedFramebuffer l = bmp.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                int width = borderBlocks ? BorderWidth : Width;
                int height = borderBlocks ? BorderHeight : Height;
                int bmpWidth = width * 16;
                int bmpHeight = height * 16;
                RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, 0xFF000000);
                Block[][] arr = borderBlocks ? BorderBlocks : Blocks;
                for (int y = 0; y < height; y++)
                {
                    Block[] arrY = arr[y];
                    for (int x = 0; x < width; x++)
                    {
                        arrY[x].BlocksetBlock.Draw(bmpAddress, bmpWidth, bmpHeight, x * 16, y * 16);
                    }
                }
            }
            OnDrew?.Invoke(this, borderBlocks);
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
                        Blocks[y][x].Write(false, w);
                    }
                }
                w.Write(BorderWidth);
                w.Write(BorderHeight);
                for (int y = 0; y < BorderHeight; y++)
                {
                    for (int x = 0; x < BorderWidth; x++)
                    {
                        BorderBlocks[y][x].Write(true, w);
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
            BlocksBitmap.Dispose();
            BorderBlocksBitmap.Dispose();
        }
    }
}
