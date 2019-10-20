using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
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
        private readonly TilesetImage _tilesetImage;
        private readonly BlocksetImage _blocksetImage;

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

            _tilesetImage = this.FindControl<TilesetImage>("TilesetImage");
            _tilesetImage.Tileset = _tileset;

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = _blockset;
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

        protected override void HandleClosed()
        {
            _tileset.DeductReference();
            _blockset.DeductReference();
            base.HandleClosed();
        }
    }
}
