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
        }

        internal void SetBlock(Blockset.Block block)
        {
            if (block != null)
            {
                _block = block;
                UpdateBitmap();
            }
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

        internal static Blockset.Block.Tile GetTile(Blockset.Block block, byte zLayerNum, byte subLayerNum, bool left, bool top)
        {
            Blockset.Block.Tile Get(Dictionary<byte, List<Blockset.Block.Tile>> dict)
            {
                List<Blockset.Block.Tile> layers = dict[zLayerNum];
                return layers.Count <= subLayerNum ? null : layers[subLayerNum];
            }
            if (top)
            {
                if (left)
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
                if (left)
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
                RenderUtil.TransparencyGrid(bmpAddress, 16, 16, 4, 4);
                GetTile(block, zLayerNum, subLayerNum, true, true)?.Draw(bmpAddress, 16, 16, 0, 0);
                GetTile(block, zLayerNum, subLayerNum, false, true)?.Draw(bmpAddress, 16, 16, 8, 0);
                GetTile(block, zLayerNum, subLayerNum, true, false)?.Draw(bmpAddress, 16, 16, 0, 8);
                GetTile(block, zLayerNum, subLayerNum, false, false)?.Draw(bmpAddress, 16, 16, 8, 8);
            }
        }

        public void Dispose()
        {
            Bitmap.Dispose();
        }
    }
}
