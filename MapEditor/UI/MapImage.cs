using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class MapImage : Control, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private readonly bool _borderBlocks;
        private Map _map;
        internal Map Map
        {
            get => _map;
            set
            {
                if (_map != value)
                {
                    Map old = _map;
                    _map = value;
                    UpdateMap(old);
                    OnPropertyChanged(nameof(Map));
                }
            }
        }

        internal Blockset.Block[][] Selection;
        private bool _isDrawing;

        public MapImage(bool borderBlocks)
        {
            _borderBlocks = borderBlocks;
        }

        private void UpdateMap(Map old)
        {
            if (old != null)
            {
                old.OnDrew -= Map_OnDrew;
            }
            if (_map != null)
            {
                _map.OnDrew += Map_OnDrew;
            }
            InvalidateMeasure();
            InvalidateVisual();
        }
        private void Map_OnDrew(Map map, bool drewBorderBlocks)
        {
            if (_borderBlocks == drewBorderBlocks)
            {
                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context)
        {
            if (_map != null)
            {
                IBitmap source = _borderBlocks ? _map.BorderBlocksBitmap : _map.BlocksBitmap;
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
                PixelSize sourcePixelSize = (_borderBlocks ? _map.BorderBlocksBitmap : _map.BlocksBitmap).PixelSize;
                return new Size(sourcePixelSize.Width, sourcePixelSize.Height);
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_map != null)
            {
                PixelSize sourcePixelSize = (_borderBlocks ? _map.BorderBlocksBitmap : _map.BlocksBitmap).PixelSize;
                return new Size(sourcePixelSize.Width, sourcePixelSize.Height);
            }
            return new Size();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_map != null)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _isDrawing = true;
                        _map.Paste(_borderBlocks, Selection, (int)pos.X / 16, (int)pos.Y / 16);
                        InvalidateVisual();
                        e.Handled = true;
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
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _map.Paste(_borderBlocks, Selection, (int)pos.X / 16, (int)pos.Y / 16);
                        InvalidateVisual();
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
