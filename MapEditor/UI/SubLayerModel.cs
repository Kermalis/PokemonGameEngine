using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.MapEditor.UI
{
    public sealed class SubLayerModel : IDisposable
    {
        private Blockset.Block _block;
        private byte _zLayerNum;
        private readonly byte _subLayerNum;
        public string Text { get; }
        public WriteableBitmap Bitmap { get; }

        internal SubLayerModel(Blockset.Block block, byte zLayerNum, byte subLayerNum)
        {
            _block = block;
            _zLayerNum = zLayerNum;
            _subLayerNum = subLayerNum;
            Text = $"Sub-Layer {_subLayerNum:D3}";
            Bitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888);
            UpdateBitmap();
        }

        internal void SetBlock(Blockset.Block block)
        {
            _block = block;
            UpdateBitmap();
        }
        internal void SetZLayer(byte z)
        {
            if (_zLayerNum != z)
            {
                _zLayerNum = z;
                UpdateBitmap();
            }
        }
        internal void UpdateBitmap()
        {
            UpdateBitmap(Bitmap, _block, _zLayerNum, _subLayerNum);
        }

        internal static Blockset.Block.Tile GetTile(Blockset.Block block, byte zLayerNum, byte subLayerNum, int x, int y)
        {
            Blockset.Block.Tile Get(Dictionary<byte, List<Blockset.Block.Tile>> dict)
            {
                List<Blockset.Block.Tile> layers = dict[zLayerNum];
                return layers.Count <= subLayerNum ? null : layers[subLayerNum];
            }
            if (y == 0)
            {
                if (x == 0)
                {
                    return Get(block.TopLeft);
                }
                else
                {
                    return Get(block.TopRight);
                }
            }
            else
            {
                if (x == 0)
                {
                    return Get(block.BottomLeft);
                }
                else
                {
                    return Get(block.BottomRight);
                }
            }
        }
        internal static unsafe void UpdateBitmap(WriteableBitmap bitmap, Blockset.Block block, byte zLayerNum, byte subLayerNum)
        {
            using (ILockedFramebuffer l = bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                for (int y = 0; y < 2; y++)
                {
                    int py = y * 8;
                    for (int x = 0; x < 2; x++)
                    {
                        int px = x * 8;
                        Blockset.Block.Tile t = GetTile(block, zLayerNum, subLayerNum, x, y);
                        if (t != null)
                        {
                            RenderUtil.TransparencyGrid(bmpAddress, 16, 16, px, py, 4, 4, 2, 2);
                            t.Draw(bmpAddress, 16, 16, px, py);
                        }
                        else
                        {
                            RenderUtil.ClearUnchecked(bmpAddress, 16, px, py, 8, 8);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Bitmap.Dispose();
        }
    }
}
