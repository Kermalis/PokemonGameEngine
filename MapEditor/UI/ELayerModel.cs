using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;

namespace Kermalis.MapEditor.UI
{
    public sealed class ELayerModel : IDisposable
    {
        private readonly byte _eLayerNum;
        private Blockset.Block _block;
        public string Text { get; }
        public WriteableBitmap Bitmap { get; }

        internal ELayerModel(byte eLayerNum)
        {
            _eLayerNum = eLayerNum;
            Text = $"E-Layer {_eLayerNum:D3}";
            Bitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888);
        }

        internal void SetBlock(Blockset.Block block)
        {
            _block = block;
            UpdateBitmap();
        }
        internal unsafe void UpdateBitmap()
        {
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtil.TransparencyGrid(bmpAddress, 16, 16, 0, 0, 4, 4, 4, 4);
                _block.Draw(bmpAddress, 16, 16, 0, 0, _eLayerNum);
            }
        }

        public void Dispose()
        {
            Bitmap.Dispose();
        }
    }
}
