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

        private readonly BlocksetImage _blocksetImage;
        private readonly TilesetImage _tilesetImage;

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
            Layers = new ZLayerModel[byte.MaxValue + 1];
            for (int i = 0; i < Layers.Length; i++)
            {
                Layers[i] = new ZLayerModel(i);
            }

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = Blockset.LoadOrGet("Test");

            _tilesetImage = this.FindControl<TilesetImage>("TilesetImage");
            _tilesetImage.Tileset = Tileset.LoadOrGet("TestTiles");
        }

        private void BlocksetImage_SelectionCompleted(object sender, Blockset.Block[][] e)
        {
            Blockset.Block b = e[0][0];
            if (b != null)
            {
                for (int i = 0; i < Layers.Length; i++)
                {
                    Layers[i].UpdateBlock(b);
                }
            }
        }
    }
}
