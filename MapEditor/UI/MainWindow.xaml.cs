using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using ReactiveUI;
using System;

namespace Kermalis.MapEditor.UI
{
    public sealed class MainWindow : Window, IDisposable
    {
        public ReactiveCommand OpenBlockEditorCommand { get; }
#pragma warning disable IDE0069 // Disposable fields should be disposed
        private BlockEditor _blockEditor;
#pragma warning restore IDE0069 // Disposable fields should be disposed

        private readonly Map _map;

        private readonly Tileset _tempTileset;
        private readonly Blockset _blockset;
        private readonly BlocksetImage _blocksetImage;
        private readonly MapImage _mapImage;

        public MainWindow()
        {
            OpenBlockEditorCommand = ReactiveCommand.Create(OpenBlockEditor);

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
            if (_blockEditor != null)
            {
                if (_blockEditor.WindowState == WindowState.Minimized)
                {
                    _blockEditor.WindowState = WindowState.Normal;
                }
                _blockEditor.Activate();
            }
            else
            {
                _blockEditor = new BlockEditor();
                _blockEditor.Show();
                _blockEditor.Closed += BlockEditor_Closed;
            }
        }
        private void BlockEditor_Closed(object sender, EventArgs e)
        {
            _blockEditor = null;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("New");
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _map.Save("TestMapC");
        }

        protected override void HandleClosed()
        {
            Dispose();
            base.HandleClosed();
        }

        public void Dispose()
        {
            _blockEditor?.Close();
            _map.Dispose();
            _blocksetImage.Dispose();
        }
    }
}
