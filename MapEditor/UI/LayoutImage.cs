using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.Overworld;
using System;

namespace Kermalis.MapEditor.UI
{
    public sealed class LayoutImage : Control, IDisposable
    {
        private readonly bool _borderBlocks;
        private Map.Layout _layout;
        internal Map.Layout Layout
        {
            get => _layout;
            set
            {
                if (_layout != value)
                {
                    RemoveLayoutEvents();
                    _layout = value;
                    value.OnDrew += MapLayout_OnDrew;
                    InvalidateMeasure();
                    InvalidateVisual();
                }
            }
        }
        private bool _showGrid;
        internal bool ShowGrid
        {
            get => _showGrid;
            set
            {
                if (value != _showGrid)
                {
                    _showGrid = value;
                    InvalidateVisual();
                }
            }
        }

        internal Blockset.Block[][] Selection;
        internal event EventHandler<Blockset.Block> SelectionCompleted;
        private bool _isDrawing;

        public LayoutImage(bool borderBlocks)
        {
            _borderBlocks = borderBlocks;
        }

        private void MapLayout_OnDrew(Map.Layout layout, bool drewBorderBlocks, bool wasResized)
        {
            if (_borderBlocks == drewBorderBlocks)
            {
                if (wasResized)
                {
                    InvalidateMeasure();
                }
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context)
        {
            if (_layout == null)
            {
                return;
            }
            IBitmap source = _borderBlocks ? _layout.BorderBlocksBitmap : _layout.BlocksBitmap;
            var viewPort = new Rect(Bounds.Size);
            var r = new Rect(source.Size);
            Rect destRect = viewPort.CenterRect(r).Intersect(viewPort);
            Rect sourceRect = r.CenterRect(new Rect(destRect.Size));

            context.DrawImage(source, 1, sourceRect, destRect);

            if (_showGrid)
            {
                int width = _borderBlocks ? _layout.BorderWidth : _layout.Width;
                int height = _borderBlocks ? _layout.BorderHeight : _layout.Height;
                StandardGrid.RenderGrid(context, width, height, OverworldConstants.Block_NumPixelsX, OverworldConstants.Block_NumPixelsY);
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_layout != null)
            {
                return (_borderBlocks ? _layout.BorderBlocksBitmap : _layout.BlocksBitmap).Size;
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_layout != null)
            {
                return (_borderBlocks ? _layout.BorderBlocksBitmap : _layout.BlocksBitmap).Size;
            }
            return new Size();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_layout == null)
            {
                return;
            }
            PointerPoint pp = e.GetCurrentPoint(this);
            switch (pp.Properties.PointerUpdateKind)
            {
                case PointerUpdateKind.LeftButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        int destX = (int)pos.X / OverworldConstants.Block_NumPixelsX;
                        int destY = (int)pos.Y / OverworldConstants.Block_NumPixelsY;
                        _isDrawing = true;
                        _layout.Paste(_borderBlocks, Selection, destX, destY);
                        e.Handled = true;
                    }
                    break;
                }
                case PointerUpdateKind.MiddleButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        int destX = (int)pos.X / OverworldConstants.Block_NumPixelsX;
                        int destY = (int)pos.Y / OverworldConstants.Block_NumPixelsY;
                        Blockset.Block oldBlock = (_borderBlocks ? _layout.BorderBlocks : _layout.Blocks)[destY][destX].BlocksetBlock;
                        Blockset.Block newBlock = Selection[0][0];
                        if (oldBlock != newBlock)
                        {
                            _layout.Fill(_borderBlocks, oldBlock, newBlock, destX, destY);
                        }
                        e.Handled = true;
                    }
                    break;
                }
                case PointerUpdateKind.RightButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        int destX = (int)pos.X / OverworldConstants.Block_NumPixelsX;
                        int destY = (int)pos.Y / OverworldConstants.Block_NumPixelsY;
                        Blockset.Block block = (_borderBlocks ? _layout.BorderBlocks : _layout.Blocks)[destY][destX].BlocksetBlock;
                        SelectionCompleted?.Invoke(this, block);
                        e.Handled = true;
                    }
                    break;
                }
            }
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_layout != null && _isDrawing)
            {
                PointerPoint pp = e.GetCurrentPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        int destX = (int)pos.X / OverworldConstants.Block_NumPixelsX;
                        int destY = (int)pos.Y / OverworldConstants.Block_NumPixelsY;
                        _layout.Paste(_borderBlocks, Selection, destX, destY);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_layout != null && _isDrawing)
            {
                PointerPoint pp = e.GetCurrentPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                {
                    _isDrawing = false;
                    e.Handled = true;
                }
            }
        }

        private void RemoveLayoutEvents()
        {
            if (_layout != null)
            {
                _layout.OnDrew -= MapLayout_OnDrew;
            }
        }
        public void Dispose()
        {
            RemoveLayoutEvents();
            SelectionCompleted = null;
        }
    }
}
