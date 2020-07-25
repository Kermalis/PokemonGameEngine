using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class LayoutEditor : UserControl, IDisposable, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

#pragma warning disable IDE0069 // Disposable fields should be disposed
        private Blockset _blockset;
#pragma warning restore IDE0069 // Disposable fields should be disposed
        private string _selectedBlockset;
        public string SelectedBlockset
        {
            get => _selectedBlockset;
            set
            {
                if (_selectedBlockset != value)
                {
                    _selectedBlockset = value;
                    RemoveBlocksetEvents();
                    _blockset = Blockset.LoadOrGet(value);
                    _blockset.OnChanged += Blockset_OnChanged;
                    _blockset.OnRemoved += Blockset_OnRemoved;
                    _blocksetImage.Blockset = _blockset;
                    OnPropertyChanged(nameof(SelectedBlockset));
                }
            }
        }

        private readonly BlocksetImage _blocksetImage;
        private readonly LayoutImage _layoutBlocksImage;
        private readonly LayoutImage _layoutBorderBlocksImage;

        private Map.Layout _layout;

        public LayoutEditor()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _layoutBlocksImage = this.FindControl<LayoutImage>("LayoutBlocksImage");
            _layoutBlocksImage.SelectionCompleted += LayoutImage_SelectionCompleted;
            _layoutBorderBlocksImage = this.FindControl<LayoutImage>("LayoutBorderBlocksImage");
            _layoutBorderBlocksImage.SelectionCompleted += LayoutImage_SelectionCompleted;
            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            SelectedBlockset = Blockset.Ids[0];
        }

        internal void SetLayout(Map.Layout layout)
        {
            _layout = layout;
            _layoutBlocksImage.Layout = layout;
            _layoutBorderBlocksImage.Layout = layout;
        }

        private void UpdateLayoutBlock(Blockset blockset, Blockset.Block block, bool resetBlock)
        {
            List<Map.Layout.Block> list = Map.Layout.DrawList;
            void Do(bool borderBlocks)
            {
                Map.Layout.Block[][] arr = borderBlocks ? _layout.BorderBlocks : _layout.Blocks;
                int width = borderBlocks ? _layout.BorderWidth : _layout.Width;
                int height = borderBlocks ? _layout.BorderHeight : _layout.Height;
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
                _layout.Draw(borderBlocks);
            }
            Do(false);
            Do(true);
        }
        private void Blockset_OnChanged(Blockset blockset, Blockset.Block block)
        {
            UpdateLayoutBlock(blockset, block, false);
        }
        private void Blockset_OnRemoved(Blockset blockset, Blockset.Block block)
        {
            UpdateLayoutBlock(blockset, block, true);
        }
        private void BlocksetImage_SelectionCompleted(object sender, Blockset.Block[][] e)
        {
            _layoutBlocksImage.Selection = e;
            _layoutBorderBlocksImage.Selection = e;
        }
        private void LayoutImage_SelectionCompleted(object sender, Blockset.Block e)
        {
            _blocksetImage.SelectBlock(_blockset.Blocks.IndexOf(e));
        }

        private void RemoveBlocksetEvents()
        {
            if (_blockset != null)
            {
                _blockset.OnChanged -= Blockset_OnChanged;
                _blockset.OnRemoved -= Blockset_OnRemoved;
            }
        }
        public void Dispose()
        {
            PropertyChanged = null;
            RemoveBlocksetEvents();
            _layoutBlocksImage.Dispose();
            _layoutBorderBlocksImage.Dispose();
            _blocksetImage.Dispose();
        }
    }
}
