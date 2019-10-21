using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class BlockEditor : Window, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private readonly Tileset _tileset;
        private readonly Blockset _blockset;
        private readonly TileLayerImage _tileLayerImage;
        private readonly TilesetImage _tilesetImage;
        private readonly BlocksetImage _blocksetImage;

        private readonly WriteableBitmap _selectionBitmap;
        private readonly Image _selectionImage;

        public ZLayerModel[] Layers { get; }
        private int _selectedLayerIndex;
        public int SelectedLayerIndex
        {
            get => _selectedLayerIndex;
            set
            {
                if (value != -1 && _selectedLayerIndex != value)
                {
                    _selectedLayerIndex = value;
                    OnPropertyChanged(nameof(SelectedLayerIndex));
                    _tileLayerImage.SetZLayer((byte)_selectedLayerIndex);
                }
            }
        }

        public BlockEditor()
        {
            _tileset = Tileset.LoadOrGet("TestTiles");
            _blockset = Blockset.LoadOrGet("TestBlocks", _tileset.Tiles[0]);

            _selectionBitmap = new WriteableBitmap(new PixelSize(8, 8), new Vector(96, 96), PixelFormat.Bgra8888);

            Layers = new ZLayerModel[byte.MaxValue + 1];
            byte z = 0;
            while (true)
            {
                Layers[z] = new ZLayerModel(z);
                if (z == byte.MaxValue)
                {
                    break;
                }
                z++;
            }

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _selectionImage = this.FindControl<Image>("SelectionImage");
            _selectionImage.Source = _selectionBitmap;

            _tileLayerImage = this.FindControl<TileLayerImage>("TileLayerImage");
            _tileLayerImage.Selection.PropertyChanged += Selection_PropertyChanged;

            _tilesetImage = this.FindControl<TilesetImage>("TilesetImage");
            _tilesetImage.SelectionCompleted += TilesetImage_SelectionCompleted;
            _tilesetImage.Tileset = _tileset;

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = _blockset;
        }

        private unsafe void Selection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            using (ILockedFramebuffer l = _selectionBitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtil.TransparencyGrid(bmpAddress, 8, 8, 4, 4);
                _tileLayerImage.Selection.Draw(bmpAddress, 8, 8, 0, 0);
            }
            _selectionImage.InvalidateVisual();
        }
        private void TilesetImage_SelectionCompleted(object sender, Tileset.Tile e)
        {
            if (e != null)
            {
                _tileLayerImage.Selection.TilesetTile = e;
            }
        }
        private void BlocksetImage_SelectionCompleted(object sender, Blockset.Block[][] e)
        {
            Blockset.Block b = e[0][0];
            if (b != null)
            {
                _tileLayerImage.SetBlock(b);
                for (int i = 0; i < Layers.Length; i++)
                {
                    Layers[i].SetBlock(b);
                }
            }
        }

        protected override void HandleClosed()
        {
            _tileset.DeductReference();
            _blockset.DeductReference();
            base.HandleClosed();
        }
    }
}
