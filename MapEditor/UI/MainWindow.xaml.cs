using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;

namespace Kermalis.MapEditor.UI
{
    public sealed class MainWindow : Window, IDisposable
    {
        public ReactiveCommand<Unit, Unit> OpenBlockEditorCommand { get; }

#pragma warning disable IDE0069 // Disposable fields should be disposed
        private BlockEditor _blockEditor;
        private readonly Blockset _blockset;
#pragma warning restore IDE0069 // Disposable fields should be disposed

        private readonly Map _map;
        private readonly BlocksetImage _blocksetImage;
        private readonly MapImage _mapBlocksImage;
        private readonly MapImage _mapBorderBlocksImage;
        private readonly ConnectionEditor _connectionEditor;

        public MainWindow()
        {
            OpenBlockEditorCommand = ReactiveCommand.Create(OpenBlockEditor);

            const string defaultBlocksetName = "TestBlocksetO"; // TODO: We will have a ComboBox with the available blocksets, and if there are none, it will prompt for a name
            _blockset = Blockset.IsValidName(defaultBlocksetName) ? new Blockset(defaultBlocksetName) : Blockset.LoadOrGet(defaultBlocksetName);
            _blockset.OnChanged += Blockset_OnChanged;
            _blockset.OnRemoved += Blockset_OnRemoved;
            _map = Map.LoadOrGet("TestMapC");
            //_map = new Map("TestMapW", new Map.Layout("TestMap2", 16, 16, 2, 2, _blockset.Blocks[0]));

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _mapBlocksImage = this.FindControl<MapImage>("MapBlocksImage");
            _mapBlocksImage.Map = _map;
            _mapBorderBlocksImage = this.FindControl<MapImage>("MapBorderBlocksImage");
            _mapBorderBlocksImage.Map = _map;

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = _blockset;

            _connectionEditor = this.FindControl<ConnectionEditor>("ConnectionEditor");
            _connectionEditor.SetMap(_map);
        }

        private void UpdateMapLayoutBlock(Blockset blockset, Blockset.Block block, bool resetBlock)
        {
            Map.Layout layout = _map.MapLayout;
            List<Map.Layout.Block> list = Map.Layout.DrawList;
            void Do(bool borderBlocks)
            {
                Map.Layout.Block[][] arr = borderBlocks ? layout.BorderBlocks : layout.Blocks;
                int width = borderBlocks ? layout.BorderWidth : layout.Width;
                int height = borderBlocks ? layout.BorderHeight : layout.Height;
                for (int y = 0; y < height; y++)
                {
                    Map.Layout.Block[] arrY = arr[y];
                    for (int x = 0; x < width; x++)
                    {
                        Map.Layout.Block b = arrY[x];
                        if (b.BlocksetBlock == block)
                        {
                            if (resetBlock)
                            {
                                b.BlocksetBlock = blockset.Blocks[0];
                            }
                            list.Add(b);
                        }
                    }
                }
                layout.Draw(borderBlocks);
            }
            Do(false);
            Do(true);
        }
        private void Blockset_OnChanged(Blockset blockset, Blockset.Block block)
        {
            UpdateMapLayoutBlock(blockset, block, false);
        }
        private void Blockset_OnRemoved(Blockset blockset, Blockset.Block block)
        {
            UpdateMapLayoutBlock(blockset, block, true);
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
            _blockEditor.Closed -= BlockEditor_Closed;
            _blockEditor = null;
        }

        private void SaveMap(object sender, RoutedEventArgs e)
        {
            _map.MapLayout.Save();
            _map.Save();
        }

        protected override bool HandleClosing()
        {
            Dispose();
            return base.HandleClosing();
        }

        private void RemoveBlocksetEvents()
        {
            _blockset.OnChanged -= Blockset_OnChanged;
            _blockset.OnRemoved -= Blockset_OnRemoved;
        }
        public void Dispose()
        {
            RemoveBlocksetEvents();
            _mapBlocksImage.Dispose();
            _mapBorderBlocksImage.Dispose();
            _blocksetImage.Dispose();
            _blocksetImage.SelectionCompleted -= BlocksetImage_SelectionCompleted;
            _connectionEditor.Dispose();
            OpenBlockEditorCommand.Dispose();
            _blockEditor?.Close();
            _map.MapLayout.Dispose();
        }
    }
}
