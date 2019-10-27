using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;

namespace Kermalis.MapEditor.UI
{
    public sealed class TileLayerImage : Control, IDisposable
    {
        public Blockset.Block.Tile Selection { get; } = new Blockset.Block.Tile();
        private bool _isDrawing;

        private readonly int _tileLayerNum;
        private byte _zLayerNum;
        private Blockset.Block _block;
        private readonly WriteableBitmap _bitmap;
        private readonly Size _bitmapSize;
        private readonly double _scale;

        public TileLayerImage(double scale)
        {
            _scale = scale;
            _bitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888);
            _bitmapSize = new Size(16, 16);
        }

        public override void Render(DrawingContext context)
        {
            var viewPort = new Rect(Bounds.Size);
            Rect destRect = viewPort.CenterRect(new Rect(_bitmapSize * _scale)).Intersect(viewPort);
            Rect sourceRect = new Rect(_bitmapSize).CenterRect(new Rect(destRect.Size / _scale));

            context.DrawImage(_bitmap, 1, sourceRect, destRect);
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            return _bitmapSize * _scale;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            return _bitmapSize * _scale;
        }

        private Blockset.Block.Tile GetTile(bool left, bool top)
        {
            Blockset.Block.Tile Get(Dictionary<byte, List<Blockset.Block.Tile>> dict)
            {
                List<Blockset.Block.Tile> layers = dict[_zLayerNum];
                return layers.Count <= _tileLayerNum ? null : layers[_tileLayerNum];
            }
            if (top)
            {
                if (left)
                {
                    return Get(_block.TopLeft);
                }
                else
                {
                    return Get(_block.TopRight);
                }
            }
            else
            {
                if (left)
                {
                    return Get(_block.BottomLeft);
                }
                else
                {
                    return Get(_block.BottomRight);
                }
            }
        }
        private void SetTile(bool left, bool top)
        {
            void Set(Dictionary<byte, List<Blockset.Block.Tile>> dict)
            {
                List<Blockset.Block.Tile> layers = dict[_zLayerNum];
                if (layers.Count < _tileLayerNum)
                {
                    throw new InvalidOperationException();
                }
                else if (layers.Count == _tileLayerNum)
                {
                    var t = new Blockset.Block.Tile();
                    Selection.CopyTo(t);
                    layers.Add(t);
                    _block.Parent.FireChanged(_block);
                }
                else
                {
                    Blockset.Block.Tile t = layers[_tileLayerNum];
                    if (!Selection.Equals(t))
                    {
                        Selection.CopyTo(t);
                        _block.Parent.FireChanged(_block);
                    }
                }
                UpdateBitmap();
            }
            if (top)
            {
                if (left)
                {
                    Set(_block.TopLeft);
                }
                else
                {
                    Set(_block.TopRight);
                }
            }
            else
            {
                if (left)
                {
                    Set(_block.BottomLeft);
                }
                else
                {
                    Set(_block.BottomRight);
                }
            }
        }
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            PointerPoint pp = e.GetPointerPoint(this);
            switch (pp.Properties.PointerUpdateKind)
            {
                case PointerUpdateKind.LeftButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _isDrawing = true;
                        SetTile((int)(pos.X / _scale) / 8 == 0, (int)(pos.Y / _scale) / 8 == 0);
                        e.Handled = true;
                    }
                    break;
                }
                case PointerUpdateKind.RightButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        GetTile((int)(pos.X / _scale) / 8 == 0, (int)(pos.Y / _scale) / 8 == 0)?.CopyTo(Selection);
                        e.Handled = true;
                    }
                    break;
                }
            }
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_isDrawing)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        SetTile((int)(pos.X / _scale) / 8 == 0, (int)(pos.Y / _scale) / 8 == 0);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_isDrawing)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                {
                    _isDrawing = false;
                    e.Handled = true;
                }
            }
        }

        internal void SetBlock(Blockset.Block block)
        {
            if (block != null)
            {
                _block = block;
                UpdateBitmap();
            }
        }
        internal void SetZLayer(byte z)
        {
            if (_zLayerNum != z)
            {
                _zLayerNum = z;
                UpdateBitmap();
            }
        }
        internal unsafe void UpdateBitmap()
        {
            using (ILockedFramebuffer l = _bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtil.TransparencyGrid(bmpAddress, 16, 16, 4, 4);
                GetTile(true, true)?.Draw(bmpAddress, 16, 16, 0, 0);
                GetTile(false, true)?.Draw(bmpAddress, 16, 16, 8, 0);
                GetTile(true, false)?.Draw(bmpAddress, 16, 16, 0, 8);
                GetTile(false, false)?.Draw(bmpAddress, 16, 16, 8, 8);
            }
            InvalidateVisual();
        }

        public void Dispose()
        {
            _bitmap.Dispose();
        }
    }
}
