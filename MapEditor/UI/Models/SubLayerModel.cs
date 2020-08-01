using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.MapEditor.UI.Models
{
    public sealed class SubLayerModel : IDisposable
    {
        private Blockset.Block _block;
        private byte _eLayerNum;
        private readonly byte _subLayerNum;
        public string Text { get; }
        public WriteableBitmap Bitmap { get; }

        internal SubLayerModel(Blockset.Block block, byte eLayerNum, byte subLayerNum)
        {
            _block = block;
            _eLayerNum = eLayerNum;
            _subLayerNum = subLayerNum;
            Text = $"Sub-Layer {_subLayerNum:X2}";
            Bitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888);
            UpdateBitmap();
        }

        internal void SetBlock(Blockset.Block block)
        {
            _block = block;
            UpdateBitmap();
        }
        internal void SetELayer(byte e)
        {
            if (_eLayerNum != e)
            {
                _eLayerNum = e;
                UpdateBitmap();
            }
        }
        internal void UpdateBitmap()
        {
            UpdateBitmap(Bitmap, _block, _eLayerNum, _subLayerNum);
        }

        internal static Blockset.Block.Tile GetTile(Blockset.Block block, byte eLayerNum, byte subLayerNum, int x, int y)
        {
            Blockset.Block.Tile Get(Dictionary<byte, List<Blockset.Block.Tile>> dict)
            {
                List<Blockset.Block.Tile> layers = dict[eLayerNum];
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
        internal static unsafe void UpdateBitmap(WriteableBitmap bitmap, Blockset.Block block, byte eLayerNum, byte subLayerNum)
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
                        Blockset.Block.Tile t = GetTile(block, eLayerNum, subLayerNum, x, y);
                        if (t != null)
                        {
                            RenderUtils.TransparencyGrid(bmpAddress, 16, 16, px, py, 4, 4, 2, 2);
                            t.Draw(bmpAddress, 16, 16, px, py);
                        }
                        else
                        {
                            RenderUtils.ClearUnchecked(bmpAddress, 16, px, py, 8, 8);
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
