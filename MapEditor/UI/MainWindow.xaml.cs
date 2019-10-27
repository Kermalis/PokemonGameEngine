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
        private readonly Tileset _tempTileset;
        private readonly Blockset _blockset;
#pragma warning restore IDE0069 // Disposable fields should be disposed

        private readonly Map _map;
        private readonly BlocksetImage _blocksetImage;
        private readonly MapImage _mapImage;

        public MainWindow()
        {
            OpenBlockEditorCommand = ReactiveCommand.Create(OpenBlockEditor);

            _tempTileset = Tileset.LoadOrGet("TestTiles");
            const string defaultBlocksetName = "TestBlockset"; // TODO: We will have a ComboBox with the available blocksets, and if there are none, it will prompt for a name
            _blockset = Blockset.IsValidName(defaultBlocksetName) ? new Blockset(defaultBlocksetName, _tempTileset.Tiles[0]) : Blockset.LoadOrGet(defaultBlocksetName);
            _blockset.OnChanged += Blockset_OnChanged;
            _blockset.OnRemoved += Blockset_OnRemoved;
            _blockset.OnReplaced += Blockset_OnReplaced;
            _map = new Map(32, 32, _blockset.Blocks[0]);

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _mapImage = this.FindControl<MapImage>("MapImage");
            _mapImage.Map = _map;

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = _blockset;
        }

        private void Blockset_OnChanged(Blockset blockset, Blockset.Block block)
        {
            for (int y = 0; y < _map.Height; y++)
            {
                Map.Block[] arrY = _map.Blocks[y];
                for (int x = 0; x < _map.Width; x++)
                {
                    Map.Block b = arrY[x];
                    if (b.BlocksetBlock == block)
                    {
                        Map.DrawList.Add(b);
                    }
                }
            }
            _map.Draw();
        }
        private void Blockset_OnRemoved(Blockset blockset, Blockset.Block block)
        {
            for (int y = 0; y < _map.Height; y++)
            {
                Map.Block[] arrY = _map.Blocks[y];
                for (int x = 0; x < _map.Width; x++)
                {
                    Map.Block b = arrY[x];
                    if (b.BlocksetBlock == block)
                    {
                        b.BlocksetBlock = blockset.Blocks[0];
                        Map.DrawList.Add(b);
                    }
                }
            }
            _map.Draw();
        }
        private void Blockset_OnReplaced(Blockset blockset, Blockset.Block oldBlock, Blockset.Block newBlock)
        {
            for (int y = 0; y < _map.Height; y++)
            {
                Map.Block[] arrY = _map.Blocks[y];
                for (int x = 0; x < _map.Width; x++)
                {
                    Map.Block b = arrY[x];
                    if (b.BlocksetBlock == oldBlock)
                    {
                        b.BlocksetBlock = newBlock;
                        Map.DrawList.Add(b);
                    }
                }
            }
            _map.Draw();
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
            OpenBlockEditorCommand.Dispose();
            _blockEditor?.Close();
            _map.Dispose();
        }
    }
}
