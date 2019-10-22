using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using ReactiveUI;
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
        private readonly ComboBox _zLayerComboBox;

        public ReactiveCommand AddBlockCommand { get; }

        private readonly WriteableBitmap _selectionBitmap;
        private readonly Image _selectionImage;

        public ZLayerModel[] ZLayers { get; }
        private int _selectedZLayerIndex;
        public int SelectedZLayerIndex
        {
            get => _selectedZLayerIndex;
            set
            {
                if (value != -1 && _selectedZLayerIndex != value)
                {
                    _selectedZLayerIndex = value;
                    OnPropertyChanged(nameof(SelectedZLayerIndex));
                    _tileLayerImage.SetZLayer((byte)_selectedZLayerIndex);
                }
            }
        }

        public BlockEditor()
        {
            AddBlockCommand = ReactiveCommand.Create(AddBlock);

            _tileset = Tileset.LoadOrGet("TestTiles");
            _blockset = Blockset.LoadOrGet("TestBlocks", _tileset.Tiles[0]);
            _blockset.OnChanged += Blockset_OnChanged;

            _selectionBitmap = new WriteableBitmap(new PixelSize(8, 8), new Vector(96, 96), PixelFormat.Bgra8888);

            ZLayers = new ZLayerModel[byte.MaxValue + 1];
            byte z = 0;
            while (true)
            {
                ZLayers[z] = new ZLayerModel(z);
                if (z == byte.MaxValue)
                {
                    break;
                }
                z++;
            }

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _zLayerComboBox = this.FindControl<ComboBox>("ZLayerComboBox");

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

        private void AddBlock()
        {
            _blockset.Blocks.Add(new Blockset.Block(_blockset, _tileset.Tiles[0]));
            _blockset.FireChanged(true);
        }
        private void UpdateZLayerComboBox()
        {
            // This forces a redraw
            IBrush old = _zLayerComboBox.Background;
            _zLayerComboBox.Background = old.Equals(Brushes.AliceBlue) ? Brushes.AntiqueWhite : Brushes.AliceBlue;
            _zLayerComboBox.Background = old;
        }

        private void Blockset_OnChanged(object sender, bool collectionChanged)
        {
            if (!collectionChanged)
            {
                for (int i = 0; i < ZLayers.Length; i++)
                {
                    ZLayers[i].UpdateBitmap();
                }
                UpdateZLayerComboBox();
            }
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
                for (int i = 0; i < ZLayers.Length; i++)
                {
                    ZLayers[i].SetBlock(b);
                }
                UpdateZLayerComboBox();
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
