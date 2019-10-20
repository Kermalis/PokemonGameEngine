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

        private Tileset.Tile _curTile;
        private bool _xFlip;
        public bool XFlip
        {
            get => _xFlip;
            set
            {
                if (_xFlip != value)
                {
                    _xFlip = value;
                    OnPropertyChanged(nameof(XFlip));
                    UpdateCurTileBitmap();
                }
            }
        }
        private bool _yFlip;
        public bool YFlip
        {
            get => _yFlip;
            set
            {
                if (_yFlip != value)
                {
                    _yFlip = value;
                    OnPropertyChanged(nameof(YFlip));
                    UpdateCurTileBitmap();
                }
            }
        }
        private readonly WriteableBitmap _curTileBitmap;
        private readonly Image _curTileImage;

        public ZLayerModel[] Layers { get; }
        private ZLayerModel _selectedLayer;
        public ZLayerModel SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                if (_selectedLayer != value)
                {
                    _selectedLayer = value;
                    OnPropertyChanged(nameof(SelectedLayer));
                }
            }
        }

        public BlockEditor()
        {
            _tileset = Tileset.LoadOrGet("TestTiles");
            _blockset = Blockset.LoadOrGet("TestBlocks", _tileset.Tiles[0]);

            _curTileBitmap = new WriteableBitmap(new PixelSize(8, 8), new Vector(96, 96), PixelFormat.Bgra8888);

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

            _curTileImage = this.FindControl<Image>("CurTileImage");
            _curTileImage.Source = _curTileBitmap;

            _tileLayerImage = this.FindControl<TileLayerImage>("TileLayerImage");
            _tileLayerImage.SelectionCompleted += TileLayerImage_SelectionCompleted;

            _tilesetImage = this.FindControl<TilesetImage>("TilesetImage");
            _tilesetImage.SelectionCompleted += TilesetImage_SelectionCompleted;
            _tilesetImage.Tileset = _tileset;

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = _blockset;
        }
        
        private void TileLayerImage_SelectionCompleted(object sender, Blockset.Block.Tile e)
        {
            if (e != null && (e.TilesetTile != _curTile || e.XFlip != _xFlip || e.YFlip != _yFlip))
            {
                _ignoreBitmapUpdate = true;
                _curTile = e.TilesetTile;
                XFlip = e.XFlip;
                YFlip = e.YFlip;
                _ignoreBitmapUpdate = false;
                UpdateCurTileBitmap();
            }
        }
        private void TilesetImage_SelectionCompleted(object sender, Tileset.Tile e)
        {
            if (e != null && e != _curTile)
            {
                _curTile = e;
                UpdateCurTileBitmap();
            }
        }
        private void BlocksetImage_SelectionCompleted(object sender, Blockset.Block[][] e)
        {
            Blockset.Block b = e[0][0];
            if (b != null)
            {
                for (int i = 0; i < Layers.Length; i++)
                {
                    Layers[i].SetBlock(b);
                }
            }
        }

        private bool _ignoreBitmapUpdate = false;
        private unsafe void UpdateCurTileBitmap()
        {
            if (!_ignoreBitmapUpdate)
            {
                using (ILockedFramebuffer l = _curTileBitmap.Lock())
                {
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    RenderUtil.TransparencyGrid(bmpAddress, 8, 8, 4, 4);
                    RenderUtil.Draw(bmpAddress, 8, 8, 0, 0, _curTile.Colors, _xFlip, _yFlip);
                }
                _curTileImage.InvalidateVisual();
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
