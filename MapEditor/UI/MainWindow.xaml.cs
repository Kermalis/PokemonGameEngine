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
            const string defaultBlocksetName = "TestBlockset"; // TODO: We will have a ComboBox with the available blocksets, and if there are none, it will prompt for a name
            _blockset = Blockset.IsValidName(defaultBlocksetName) ? new Blockset(defaultBlocksetName, _tempTileset.Tiles[0]) : Blockset.LoadOrGet(defaultBlocksetName);
            _blockset.OnChanged += Blockset_OnChanged;
            _map = new Map(32, 32, _blockset.Blocks[0]);

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _mapImage = this.FindControl<MapImage>("MapImage");
            _mapImage.Map = _map;

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = _blockset;
        }

        private void Blockset_OnChanged(object sender, bool collectionChanged)
        {
            if (!collectionChanged)
            {
                _map.DrawAll();
            }
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
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _map.Save("TestMapC");
        }
    }
}
