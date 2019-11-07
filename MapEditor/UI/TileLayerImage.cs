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

        private byte _subLayerNum;
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

        private void SetTile(bool remove, bool left, bool top)
        {
            void Set(Dictionary<byte, List<Blockset.Block.Tile>> dict)
            {
                List<Blockset.Block.Tile> subLayers = dict[_zLayerNum];
                if (subLayers.Count < _subLayerNum)
                {
                    throw new InvalidOperationException();
                }
                else if (subLayers.Count == _subLayerNum)
                {
                    if (!remove)
                    {
                        var t = new Blockset.Block.Tile();
                        Selection.CopyTo(t);
                        subLayers.Add(t);
                        _block.Parent.FireChanged(_block);
                        UpdateBitmap();
                    }
                }
                else
                {
                    if (remove)
                    {
                        subLayers.RemoveAt(_subLayerNum);
                        _block.Parent.FireChanged(_block);
                        UpdateBitmap();
                    }
                    else
                    {
                        Blockset.Block.Tile t = subLayers[_subLayerNum];
                        if (!Selection.Equals(t))
                        {
                            Selection.CopyTo(t);
                            _block.Parent.FireChanged(_block);
                            UpdateBitmap();
                        }
                    }
                }
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
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        _isDrawing = true;
                        SetTile(false, (int)(pos.X / _scale) / 8 == 0, (int)(pos.Y / _scale) / 8 == 0);
                        e.Handled = true;
                    }
                    break;
                }
                case PointerUpdateKind.MiddleButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        SetTile(true, (int)(pos.X / _scale) / 8 == 0, (int)(pos.Y / _scale) / 8 == 0);
                        e.Handled = true;
                    }
                    break;
                }
                case PointerUpdateKind.RightButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        SubLayerModel.GetTile(_block, _zLayerNum, _subLayerNum, (int)(pos.X / _scale) / 8 == 0, (int)(pos.Y / _scale) / 8 == 0)?.CopyTo(Selection);
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
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        SetTile(false, (int)(pos.X / _scale) / 8 == 0, (int)(pos.Y / _scale) / 8 == 0);
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
        internal void SetSubLayer(byte s)
        {
            if (_subLayerNum != s)
            {
                _subLayerNum = s;
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
            SubLayerModel.UpdateBitmap(_bitmap, _block, _zLayerNum, _subLayerNum);
            InvalidateVisual();
        }

        public void Dispose()
        {
            _bitmap.Dispose();
        }
    }
}
