using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using System;
using System.Collections.Generic;

namespace Kermalis.MapEditor.UI
{
    public sealed class LayoutEditor : UserControl, IDisposable
    {
#pragma warning disable IDE0069 // Disposable fields should be disposed
        private readonly Blockset _blockset;
#pragma warning restore IDE0069 // Disposable fields should be disposed

        private readonly BlocksetImage _blocksetImage;
        private readonly LayoutImage _layoutBlocksImage;
        private readonly LayoutImage _layoutBorderBlocksImage;

        private Map.Layout _layout;

        public LayoutEditor()
        {
            const string defaultBlocksetName = "TestBlocksetO"; // TODO: We will have a ComboBox with the available blocksets, and if there are none, it will prompt for a name
            _blockset = Blockset.IsValidName(defaultBlocksetName) ? new Blockset(defaultBlocksetName) : Blockset.LoadOrGet(defaultBlocksetName);
            _blockset.OnChanged += Blockset_OnChanged;
            _blockset.OnRemoved += Blockset_OnRemoved;

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _layoutBlocksImage = this.FindControl<LayoutImage>("LayoutBlocksImage");
            _layoutBorderBlocksImage = this.FindControl<LayoutImage>("LayoutBorderBlocksImage");
            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = _blockset;
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

        private void RemoveBlocksetEvents()
        {
            _blockset.OnChanged -= Blockset_OnChanged;
            _blockset.OnRemoved -= Blockset_OnRemoved;
        }
        public void Dispose()
        {
            RemoveBlocksetEvents();
            _layoutBlocksImage.Dispose();
            _layoutBorderBlocksImage.Dispose();
            _blocksetImage.Dispose();
            _blocksetImage.SelectionCompleted -= BlocksetImage_SelectionCompleted;
        }
    }
}
