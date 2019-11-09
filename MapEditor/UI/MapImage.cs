using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;

namespace Kermalis.MapEditor.UI
{
    public sealed class MapImage : Control
    {
        private readonly bool _borderBlocks;
        private Map _map;
        internal Map Map
        {
            get => _map;
            set
            {
                if (_map != value)
                {
                    if (_map != null)
                    {
                        _map.MapLayout.OnDrew -= MapLayout_OnDrew;
                    }
                    _map = value;
                    if (_map != null)
                    {
                        _map.MapLayout.OnDrew += MapLayout_OnDrew;
                    }
                    InvalidateMeasure();
                    InvalidateVisual();
                }
            }
        }

        internal Blockset.Block[][] Selection;
        private bool _isDrawing;

        public MapImage(bool borderBlocks)
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
            if (_map != null)
            {
                IBitmap source = _borderBlocks ? _map.MapLayout.BorderBlocksBitmap : _map.MapLayout.BlocksBitmap;
                var viewPort = new Rect(Bounds.Size);
                var r = new Rect(source.Size);
                Rect destRect = viewPort.CenterRect(r).Intersect(viewPort);
                Rect sourceRect = r.CenterRect(new Rect(destRect.Size));

                context.DrawImage(source, 1, sourceRect, destRect);
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_map != null)
            {
                return (_borderBlocks ? _map.MapLayout.BorderBlocksBitmap : _map.MapLayout.BlocksBitmap).Size;
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_map != null)
            {
                return (_borderBlocks ? _map.MapLayout.BorderBlocksBitmap : _map.MapLayout.BlocksBitmap).Size;
            }
            return new Size();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_map != null)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                switch (pp.Properties.PointerUpdateKind)
                {
                    case PointerUpdateKind.LeftButtonPressed:
                    {
                        Point pos = pp.Position;
                        if (Bounds.TemporaryFix_PointerInControl(pos))
                        {
                            _isDrawing = true;
                            _map.MapLayout.Paste(_borderBlocks, Selection, (int)pos.X / 16, (int)pos.Y / 16);
                            e.Handled = true;
                        }
                        break;
                    }
                    case PointerUpdateKind.MiddleButtonPressed:
                    {
                        Point pos = pp.Position;
                        if (Bounds.TemporaryFix_PointerInControl(pos))
                        {
                            int x = (int)pos.X / 16;
                            int y = (int)pos.Y / 16;
                            Map.Layout ml = _map.MapLayout;
                            Blockset.Block oldBlock = (_borderBlocks ? ml.BorderBlocks : ml.Blocks)[y][x].BlocksetBlock;
                            Blockset.Block newBlock = Selection[0][0];
                            if (oldBlock != newBlock)
                            {
                                ml.Fill(_borderBlocks, oldBlock, newBlock, x, y);
                            }
                            e.Handled = true;
                        }
                        break;
                    }
                }
            }
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_map != null && _isDrawing)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        _map.MapLayout.Paste(_borderBlocks, Selection, (int)pos.X / 16, (int)pos.Y / 16);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_map != null && _isDrawing)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                {
                    _isDrawing = false;
                    e.Handled = true;
                }
            }
        }
    }
}
