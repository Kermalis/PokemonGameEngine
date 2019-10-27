using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class TilesetImage : Control, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private readonly double _scale;

        private bool _isSelecting;
        private readonly Selection _selection;
        internal event EventHandler<Tileset.Tile> SelectionCompleted;

        private Tileset _tileset;
        internal Tileset Tileset
        {
            get => _tileset;
            set
            {
                if (_tileset != value)
                {
                    _tileset = value;
                    _isSelecting = false;
                    _selection.Start(0, 0, 1, 1);
                    FireSelectionCompleted();
                    InvalidateMeasure();
                    InvalidateVisual();
                    OnPropertyChanged(nameof(Tileset));
                }
            }
        }

        public TilesetImage(double scale)
        {
            _scale = scale;
            _selection = new Selection();
            _selection.Changed += OnSelectionChanged;
        }

        public override void Render(DrawingContext context)
        {
            if (_tileset != null)
            {
                IBitmap source = _tileset.Bitmap;
                var viewPort = new Rect(Bounds.Size);
                Size sourceSize = source.Size;
                Rect destRect = viewPort.CenterRect(new Rect(sourceSize * _scale)).Intersect(viewPort);
                Rect sourceRect = new Rect(sourceSize).CenterRect(new Rect(destRect.Size / _scale));

                context.DrawImage(source, 1, sourceRect, destRect);
                var r = new Rect(_selection.X * 8 * _scale, _selection.Y * 8 * _scale, _selection.Width * 8 * _scale, _selection.Height * 8 * _scale);
                context.FillRectangle(_isSelecting ? Selection.SelectingBrush : Selection.SelectionBrush, r);
                context.DrawRectangle(_isSelecting ? Selection.SelectingPen : Selection.SelectionPen, r);
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_tileset != null)
            {
                return _tileset.Bitmap.Size * _scale;
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_tileset != null)
            {
                return _tileset.Bitmap.Size * _scale;
            }
            return new Size();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_tileset != null)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _isSelecting = true;
                        _selection.Start((int)(pos.X / _scale) / 8, (int)(pos.Y / _scale) / 8, 1, 1);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_tileset != null && _isSelecting)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                {
                    _isSelecting = false;
                    FireSelectionCompleted();
                    InvalidateVisual();
                    e.Handled = true;
                }
            }
        }
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
        }
        private void FireSelectionCompleted()
        {
            if (SelectionCompleted != null)
            {
                int index = _selection.X + (_selection.Y * Tileset.BitmapNumTilesX);
                SelectionCompleted.Invoke(this, index >= _tileset.Tiles.Length ? null : _tileset.Tiles[index]);
            }
        }
    }
}
