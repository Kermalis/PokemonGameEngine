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
    public sealed class TilesetImage : Control, IDisposable
    {
        private readonly double _scale;

        private bool _isSelecting;
        private readonly Selection _selection;
        internal event EventHandler<Tileset.Tile[][]> SelectionCompleted;

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
                }
            }
        }

        public TilesetImage(double scale)
        {
            _scale = scale;
            _selection = new Selection(OverworldConstants.Block_NumTilesX, OverworldConstants.Block_NumTilesY);
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
                var r = new Rect(_selection.X * OverworldConstants.Tile_NumPixelsX * _scale, _selection.Y * OverworldConstants.Tile_NumPixelsY * _scale, _selection.Width * OverworldConstants.Tile_NumPixelsX * _scale, _selection.Height * OverworldConstants.Tile_NumPixelsY * _scale);
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
                PointerPoint pp = e.GetCurrentPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        _isSelecting = true;
                        _selection.Start((int)(pos.X / _scale) / OverworldConstants.Tile_NumPixelsX, (int)(pos.Y / _scale) / OverworldConstants.Tile_NumPixelsY, 1, 1);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_tileset != null && _isSelecting)
            {
                PointerPoint pp = e.GetCurrentPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        _selection.Move((int)(pos.X / _scale) / OverworldConstants.Tile_NumPixelsX, (int)(pos.Y / _scale) / OverworldConstants.Tile_NumPixelsY);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_tileset != null && _isSelecting)
            {
                PointerPoint pp = e.GetCurrentPoint(this);
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
                var tiles = new Tileset.Tile[_selection.Height][];
                for (int y = 0; y < _selection.Height; y++)
                {
                    var arrY = new Tileset.Tile[_selection.Width];
                    for (int x = 0; x < _selection.Width; x++)
                    {
                        int index = x + _selection.X + ((y + _selection.Y) * _tileset.BitmapNumTilesX);
                        arrY[x] = (index >= _tileset.Tiles.Length) ? null : _tileset.Tiles[index];
                    }
                    tiles[y] = arrY;
                }
                SelectionCompleted.Invoke(this, tiles);
            }
        }

        public void Dispose()
        {
            _selection.Changed -= OnSelectionChanged;
            SelectionCompleted = null;
        }
    }
}
