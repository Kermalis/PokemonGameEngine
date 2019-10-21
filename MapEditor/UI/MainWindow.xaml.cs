using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using ReactiveUI;
using System;
using System.Reactive.Subjects;

namespace Kermalis.MapEditor.UI
{
    public sealed class MainWindow : Window
    {
        public ReactiveCommand OpenBlockEditorCommand { get; }
        private readonly Subject<bool> _openBlockEditorCanExecute;

        private readonly Map _map;

        private readonly Tileset _tempTileset;
        private readonly Blockset _blockset;
        private readonly BlocksetImage _blocksetImage;
        private readonly MapImage _mapImage;

        public MainWindow()
        {
            _openBlockEditorCanExecute = new Subject<bool>();
            OpenBlockEditorCommand = ReactiveCommand.Create(OpenBlockEditor, _openBlockEditorCanExecute);
            _openBlockEditorCanExecute.OnNext(true);

            _tempTileset = Tileset.LoadOrGet("TestTiles");
            _blockset = Blockset.LoadOrGet("TestBlocks", _tempTileset.Tiles[0]);
            _map = new Map(32, 32, _blockset.Blocks[0]);

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _mapImage = this.FindControl<MapImage>("MapImage");
            _mapImage.Map = _map;

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = _blockset;
        }

        private void BlocksetImage_SelectionCompleted(object sender, Blockset.Block[][] e)
        {
            _mapImage.Selection = e;
        }

        private void OpenBlockEditor()
        {
            _openBlockEditorCanExecute.OnNext(false);
            var be = new BlockEditor();
            be.Show();
            be.Closed += BlockEditor_Closed;
        }
        private void BlockEditor_Closed(object sender, EventArgs e)
        {
            _openBlockEditorCanExecute.OnNext(true);
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("New");
        }
    }
}
