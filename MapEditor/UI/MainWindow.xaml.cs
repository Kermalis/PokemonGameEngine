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
        private readonly Blockset _blockset;
#pragma warning restore IDE0069 // Disposable fields should be disposed

        private readonly Map _map;
        private readonly BlocksetImage _blocksetImage;
        private readonly MapImage _mapBlocksImage;
        private readonly MapImage _mapBorderBlocksImage;

        public MainWindow()
        {
            OpenBlockEditorCommand = ReactiveCommand.Create(OpenBlockEditor);

            const string defaultBlocksetName = "TestBlockset"; // TODO: We will have a ComboBox with the available blocksets, and if there are none, it will prompt for a name
            _blockset = Blockset.IsValidName(defaultBlocksetName) ? new Blockset(defaultBlocksetName) : Blockset.LoadOrGet(defaultBlocksetName);
            _blockset.OnChanged += Blockset_OnChanged;
            _blockset.OnRemoved += Blockset_OnRemoved;
            _map = new Map("TestMapC");
            //_map = new Map(32, 32, 2, 2, _blockset.Blocks[0]);

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _mapBlocksImage = this.FindControl<MapImage>("MapBlocksImage");
            _mapBlocksImage.Map = _map;
            _mapBorderBlocksImage = this.FindControl<MapImage>("MapBorderBlocksImage");
            _mapBorderBlocksImage.Map = _map;

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = _blockset;
        }

        private void UpdateMapBlock(Blockset blockset, Blockset.Block block, bool resetBlock)
        {
            void Do(bool borderBlocks)
            {
                Map.Block[][] arr = borderBlocks ? _map.BorderBlocks : _map.Blocks;
                int width = borderBlocks ? _map.BorderWidth : _map.Width;
                int height = borderBlocks ? _map.BorderHeight : _map.Height;
                for (int y = 0; y < height; y++)
                {
                    Map.Block[] arrY = arr[y];
                    for (int x = 0; x < width; x++)
                    {
                        Map.Block b = arrY[x];
                        if (b.BlocksetBlock == block)
                        {
                            if (resetBlock)
                            {
                                b.BlocksetBlock = blockset.Blocks[0];
                            }
                            Map.DrawList.Add(b);
                        }
                    }
                }
                _map.Draw(borderBlocks);
            }
            Do(false);
            Do(true);
        }
        private void Blockset_OnChanged(Blockset blockset, Blockset.Block block)
        {
            UpdateMapBlock(blockset, block, false);
        }
        private void Blockset_OnRemoved(Blockset blockset, Blockset.Block block)
        {
            UpdateMapBlock(blockset, block, true);
        }
        private void BlocksetImage_SelectionCompleted(object sender, Blockset.Block[][] e)
        {
            _mapBlocksImage.Selection = e;
            _mapBorderBlocksImage.Selection = e;
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
