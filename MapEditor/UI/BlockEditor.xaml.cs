using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;

namespace Kermalis.MapEditor.UI
{
    public sealed class BlockEditor : Window
    {
        private readonly BlocksetImage _blocksetImage;
        private readonly TilesetImage _tilesetImage;

        public BlockEditor()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.Blockset = Blockset.LoadOrGet("Test");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;

            _tilesetImage = this.FindControl<TilesetImage>("TilesetImage");
            _tilesetImage.Tileset = Tileset.LoadOrGet("TestTiles");
        }

        private void BlocksetImage_SelectionCompleted(object sender, Blockset.Block[][] e)
        {
            Blockset.Block b = e[0][0];
            if (b != null)
            {
                // 
            }
        }
    }
}
