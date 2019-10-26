using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;

namespace Kermalis.MapEditor.UI
{
    public sealed class ZLayerModel : IDisposable
    {
        private readonly byte _zLayerNum;
        private Blockset.Block _block;
        public string Text { get; }
        public WriteableBitmap Bitmap { get; }

        internal ZLayerModel(byte zLayerNum)
        {
            _zLayerNum = zLayerNum;
            Text = $"Z-Layer {_zLayerNum:D3}";
            Bitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888);
        }

        internal void SetBlock(Blockset.Block block)
        {
            if (block != null && block != _block)
            {
                _block = block;
                UpdateBitmap();
            }
        }
        internal unsafe void UpdateBitmap()
        {
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtil.TransparencyGrid(bmpAddress, 16, 16, 4, 4);
                _block.DrawZ(bmpAddress, 16, 16, 0, 0, _zLayerNum);
            }
        }

        public void Dispose()
        {
            Bitmap.Dispose();
        }
    }
}
