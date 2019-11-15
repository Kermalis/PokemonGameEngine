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
        internal readonly Blockset.Block.Tile[][] Clipboard;
        internal int ClipboardWidth;
        internal int ClipboardHeight;
        internal EventHandler ClipboardChanged;

        private bool _isDrawing;
        private bool _isSelecting;
        private readonly Selection _selection;

        private byte _subLayerNum;
        private byte _zLayerNum;
        private Blockset.Block _block;
        private readonly WriteableBitmap _bitmap;
        private readonly Size _bitmapSize;
        private readonly double _scale;

        public TileLayerImage(double scale)
        {
            _scale = scale;
            Clipboard = new Blockset.Block.Tile[2][];
            for (int y = 0; y < 2; y++)
            {
                var arrY = new Blockset.Block.Tile[2];
                for (int x = 0; x < 2; x++)
                {
                    arrY[x] = new Blockset.Block.Tile();
                }
                Clipboard[y] = arrY;
            }
            _selection = new Selection(2, 2);
            _selection.Changed += OnSelectionChanged;
            _bitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888);
            _bitmapSize = new Size(16, 16);
        }

        public override void Render(DrawingContext context)
        {
            var viewPort = new Rect(Bounds.Size);
            Rect destRect = viewPort.CenterRect(new Rect(_bitmapSize * _scale)).Intersect(viewPort);
            Rect sourceRect = new Rect(_bitmapSize).CenterRect(new Rect(destRect.Size / _scale));

            context.DrawImage(_bitmap, 1, sourceRect, destRect);
            if (_isSelecting)
            {
                var r = new Rect(_selection.X * 8 * _scale, _selection.Y * 8 * _scale, _selection.Width * 8 * _scale, _selection.Height * 8 * _scale);
                context.FillRectangle(Selection.SelectingBrush, r);
                context.DrawRectangle(Selection.SelectingPen, r);
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            return _bitmapSize * _scale;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            return _bitmapSize * _scale;
        }

        private void SetTiles(int startX, int startY)
        {
            bool changed = false;
            void Set(Dictionary<byte, List<Blockset.Block.Tile>> dict, Blockset.Block.Tile st)
            {
                List<Blockset.Block.Tile> subLayers = dict[_zLayerNum];
                if (subLayers.Count <= _subLayerNum)
                {
                    var t = new Blockset.Block.Tile();
                    st.CopyTo(t);
                    subLayers.Add(t);
                    changed = true;
                }
                else
                {
                    Blockset.Block.Tile t = subLayers[_subLayerNum];
                    if (!st.Equals(t))
                    {
                        st.CopyTo(t);
                        changed = true;
                    }
                }
            }
            for (int y = 0; y < ClipboardHeight; y++)
            {
                int curY = startY + y;
                if (curY < 2)
                {
                    Blockset.Block.Tile[] arrY = Clipboard[y];
                    for (int x = 0; x < ClipboardWidth; x++)
                    {
                        int curX = startX + x;
                        if (curX < 2)
                        {
                            Blockset.Block.Tile st = arrY[x];
                            if (curY == 0)
                            {
                                if (curX == 0)
                                {
                                    Set(_block.TopLeft, st);
                                }
                                else
                                {
                                    Set(_block.TopRight, st);
                                }
                            }
                            else
                            {
                                if (curX == 0)
                                {
                                    Set(_block.BottomLeft, st);
                                }
                                else
                                {
                                    Set(_block.BottomRight, st);
                                }
                            }
                        }
                    }
                }
            }
            if (changed)
            {
                _block.Parent.FireChanged(_block);
                UpdateBitmap();
            }
        }
        private void RemoveTile(int x, int y)
        {
            bool changed = false;
            void Remove(Dictionary<byte, List<Blockset.Block.Tile>> dict)
            {
                List<Blockset.Block.Tile> subLayers = dict[_zLayerNum];
                if (subLayers.Count > _subLayerNum)
                {
                    subLayers.RemoveAt(_subLayerNum);
                    changed = true;
                }
            }
            if (y == 0)
            {
                if (x == 0)
                {
                    Remove(_block.TopLeft);
                }
                else
                {
                    Remove(_block.TopRight);
                }
            }
            else
            {
                if (x == 0)
                {
                    Remove(_block.BottomLeft);
                }
                else
                {
                    Remove(_block.BottomRight);
                }
            }
            if (changed)
            {
                _block.Parent.FireChanged(_block);
                UpdateBitmap();
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
                        SetTiles((int)(pos.X / _scale) / 8, (int)(pos.Y / _scale) / 8);
                        e.Handled = true;
                    }
                    break;
                }
                case PointerUpdateKind.MiddleButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        RemoveTile((int)(pos.X / _scale) / 8, (int)(pos.Y / _scale) / 8);
                        e.Handled = true;
                    }
                    break;
                }
                case PointerUpdateKind.RightButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        _isSelecting = true;
                        _selection.Start((int)(pos.X / _scale) / 8, (int)(pos.Y / _scale) / 8, 1, 1);
                        InvalidateVisual();
                        e.Handled = true;
                    }
                    break;
                }
            }
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_isDrawing || _isSelecting)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        int x = (int)(pos.X / _scale) / 8;
                        int y = (int)(pos.Y / _scale) / 8;
                        if (_isDrawing)
                        {
                            SetTiles(x, y);
                        }
                        else
                        {
                            _selection.Move(x, y);
                        }
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_isDrawing || _isSelecting)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                switch (pp.Properties.PointerUpdateKind)
                {
                    case PointerUpdateKind.LeftButtonReleased:
                    {
                        _isDrawing = false;
                        e.Handled = true;
                        break;
                    }
                    case PointerUpdateKind.RightButtonReleased:
                    {
                        _isSelecting = false;
                        int startX = _selection.X;
                        int startY = _selection.Y;
                        int width = _selection.Width;
                        int height = _selection.Height;
                        bool changed = false;
                        if (ClipboardWidth != width)
                        {
                            ClipboardWidth = width;
                            changed = true;
                        }
                        if (ClipboardHeight != height)
                        {
                            ClipboardHeight = height;
                            changed = true;
                        }
                        for (int y = 0; y < height; y++)
                        {
                            Blockset.Block.Tile[] arrY = Clipboard[y];
                            for (int x = 0; x < width; x++)
                            {
                                Blockset.Block.Tile got = SubLayerModel.GetTile(_block, _zLayerNum, _subLayerNum, startX + x, startY + y);
                                if (got != null)
                                {
                                    Blockset.Block.Tile t = arrY[x];
                                    if (!got.Equals(t))
                                    {
                                        got.CopyTo(t);
                                        changed = true;
                                    }
                                }
                            }
                        }
                        if (changed)
                        {
                            ClipboardChanged?.Invoke(this, EventArgs.Empty);
                        }
                        InvalidateVisual();
                        e.Handled = true;
                        break;
                    }
                }
            }
        }
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
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
            _selection.Changed -= OnSelectionChanged;
        }
    }
}
