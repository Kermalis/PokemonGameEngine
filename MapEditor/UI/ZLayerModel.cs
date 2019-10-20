using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class ZLayerModel : INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly int _layerNum;
        private Blockset.Block _block;
        public string Text { get; }
        public WriteableBitmap Bitmap { get; }

        public ZLayerModel(int layerNum)
        {
            _layerNum = layerNum;
            Text = $"Layer {_layerNum:D3}";
            Bitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888);
        }

        public void UpdateBlock(Blockset.Block block)
        {
            if (block != null && block != _block)
            {
                _block = block;
                UpdateBitmap();
            }
        }
        public unsafe void UpdateBitmap()
        {
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtil.TransparencyGrid(bmpAddress, 16, 16, 4, 4);
                _block.DrawZ(bmpAddress, 16, 16, 0, 0, _layerNum);
            }
            OnPropertyChanged(nameof(Bitmap));
        }
    }
}
