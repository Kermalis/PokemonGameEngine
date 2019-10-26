using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class TilesetImage : Control, IDisposable, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private const int numTilesX = 8;

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
                    OnTilesetChanged();
                    OnPropertyChanged(nameof(Tileset));
                }
            }
        }

        private bool _isSelecting;

        private WriteableBitmap _bitmap;
        private Size _bitmapSize;
        private readonly double _scale;

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
                var viewPort = new Rect(Bounds.Size);
                Rect destRect = viewPort.CenterRect(new Rect(_bitmapSize * _scale)).Intersect(viewPort);
                Rect sourceRect = new Rect(_bitmapSize).CenterRect(new Rect(destRect.Size / _scale));

                context.DrawImage(_bitmap, 1, sourceRect, destRect);
                var r = new Rect(_selection.X * 8 * _scale, _selection.Y * 8 * _scale, _selection.Width * 8 * _scale, _selection.Height * 8 * _scale);
                context.FillRectangle(_isSelecting ? Selection.SelectingBrush : Selection.SelectionBrush, r);
                context.DrawRectangle(_isSelecting ? Selection.SelectingPen : Selection.SelectionPen, r);
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_tileset != null)
            {
                return _bitmapSize * _scale;
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_tileset != null)
            {
                return _bitmapSize * _scale;
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
                int index = _selection.X + (_selection.Y * numTilesX);
                SelectionCompleted.Invoke(this, index >= _tileset.Tiles.Length ? null : _tileset.Tiles[index]);
            }
        }
        private void ResetSelectionAndInvalidateVisual()
        {
            _isSelecting = false;
            _selection.Start(0, 0, 1, 1);
            InvalidateVisual();
        }
        private unsafe void OnTilesetChanged()
        {
            if (_tileset != null)
            {
                Tileset.Tile[] tiles = _tileset.Tiles;
                int numTilesY = (tiles.Length / numTilesX) + (tiles.Length % numTilesX != 0 ? 1 : 0);
                int bmpWidth = numTilesX * 8;
                int bmpHeight = numTilesY * 8;
                if (_bitmap == null || _bitmap.PixelSize.Height != bmpHeight)
                {
                    _bitmap?.Dispose();
                    _bitmap = new WriteableBitmap(new PixelSize(bmpWidth, bmpHeight), new Vector(96, 96), PixelFormat.Bgra8888);
                    _bitmapSize = new Size(bmpWidth, bmpHeight);
                }
                using (ILockedFramebuffer l = _bitmap.Lock())
                {
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    RenderUtil.TransparencyGrid(bmpAddress, bmpWidth, bmpHeight, 4, 4);
                    int x = 0;
                    int y = 0;
                    for (int i = 0; i < tiles.Length; i++, x++)
                    {
                        if (x >= numTilesX)
                        {
                            x = 0;
                            y++;
                        }
                        RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, x * 8, y * 8, tiles[i].Colors, false, false);
                    }
                    for (; x < numTilesX; x++)
                    {
                        RenderUtil.DrawCrossUnchecked(bmpAddress, bmpWidth, x * 8, y * 8, 8, 8, 0xFFFF0000);
                    }
                }
                ResetSelectionAndInvalidateVisual();
                FireSelectionCompleted();
            }
        }

        public void Dispose()
        {
            _bitmap.Dispose();
        }
    }
}
