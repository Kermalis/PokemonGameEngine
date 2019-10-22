using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Util;
using System;

namespace Kermalis.MapEditor.Core
{
    internal sealed class Map
    {
        public sealed class Block
        {
            public byte Behavior;
            public Blockset.Block BlocksetBlock;
        }

        public WriteableBitmap Bitmap;
        public event EventHandler<EventArgs> OnDrew;

        public int Width;
        public int Height;

        public Block[][] Blocks;

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
                    arrY[x] = new Block() { BlocksetBlock = defaultBlock };
                }
                Blocks[y] = arrY;
            }

            Draw();
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
                                outArrY[dx].BlocksetBlock = b;
                            }
                        }
                    }
                }
            }
            Draw();
        }

        public unsafe void Draw()
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
                Bitmap = new WriteableBitmap(new PixelSize(Width * 16, Height * 16), new Vector(96, 96), PixelFormat.Bgra8888);
            }
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
    }
}
