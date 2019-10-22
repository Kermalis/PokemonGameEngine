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
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class BlocksetImage : Control, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private const int numBlocksX = 8;

        private readonly bool _allowSelectingMultiple;

        private readonly Selection _selection;
        public event EventHandler<Blockset.Block[][]> SelectionCompleted;

        private Blockset _blockset;
        public Blockset Blockset
        {
            get => _blockset;
            set
            {
                if (_blockset != value)
                {
                    if (_blockset != null)
                    {
                        _blockset.OnChanged -= OnBlocksetChanged;
                    }
                    _blockset = value;
                    _blockset.OnChanged += OnBlocksetChanged;
                    UpdateBlockset(true);
                    OnPropertyChanged(nameof(Blockset));
                }
            }
        }

        private bool _isSelecting;

        private WriteableBitmap _bitmap;
        private Size _bitmapSize;
        private readonly double _scale;

        public BlocksetImage(bool allowSelectingMultiple, double scale)
        {
            _allowSelectingMultiple = allowSelectingMultiple;
            _scale = scale;
            _selection = new Selection();
            _selection.Changed += OnSelectionChanged;
        }

        public override void Render(DrawingContext context)
        {
            if (_blockset != null)
            {
                var viewPort = new Rect(Bounds.Size);
                Rect destRect = viewPort.CenterRect(new Rect(_bitmapSize * _scale)).Intersect(viewPort);
                Rect sourceRect = new Rect(_bitmapSize).CenterRect(new Rect(destRect.Size / _scale));

                context.DrawImage(_bitmap, 1, sourceRect, destRect);
                var r = new Rect(_selection.X * 16 * _scale, _selection.Y * 16 * _scale, _selection.Width * 16 * _scale, _selection.Height * 16 * _scale);
                context.FillRectangle(_isSelecting ? Selection.SelectingBrush : Selection.SelectionBrush, r);
                context.DrawRectangle(_isSelecting ? Selection.SelectingPen : Selection.SelectionPen, r);
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_blockset != null)
            {
                return _bitmapSize * _scale;
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_blockset != null)
            {
                return _bitmapSize * _scale;
            }
            return new Size();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_blockset != null)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _isSelecting = true;
                        _selection.Start((int)(pos.X / _scale) / 16, (int)(pos.Y / _scale) / 16, 1, 1);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_blockset != null && _isSelecting && _allowSelectingMultiple)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _selection.Move((int)(pos.X / _scale) / 16, (int)(pos.Y / _scale) / 16);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_blockset != null && _isSelecting)
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
        private void OnBlocksetChanged(object sender, bool collectionChanged)
        {
            UpdateBlockset(collectionChanged);
        }
        private void FireSelectionCompleted()
        {
            if (SelectionCompleted != null)
            {
                var blocks = new Blockset.Block[_selection.Height][];
                for (int y = 0; y < _selection.Height; y++)
                {
                    var arrY = new Blockset.Block[_selection.Width];
                    for (int x = 0; x < _selection.Width; x++)
                    {
                        int index = x + _selection.X + ((y + _selection.Y) * numBlocksX);
                        arrY[x] = (index >= _blockset.Blocks.Count) ? null : _blockset.Blocks[index];
                    }
                    blocks[y] = arrY;
                }
                SelectionCompleted.Invoke(this, blocks);
            }
        }
        private unsafe void UpdateBlockset(bool collectionChanged)
        {
            if (_blockset != null)
            {
                List<Blockset.Block> blocks = _blockset.Blocks;
                int numBlocksY = (blocks.Count / numBlocksX) + (blocks.Count % numBlocksX != 0 ? 1 : 0);
                int bmpWidth = numBlocksX * 16;
                int bmpHeight = numBlocksY * 16;
                if (_bitmap == null || _bitmap.PixelSize.Height != bmpHeight)
                {
                    _bitmap = new WriteableBitmap(new PixelSize(bmpWidth, bmpHeight), new Vector(96, 96), PixelFormat.Bgra8888);
                    _bitmapSize = new Size(bmpWidth, bmpHeight);
                }
                using (ILockedFramebuffer l = _bitmap.Lock())
                {
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, 0xFF000000);
                    int x = 0;
                    int y = 0;
                    for (int i = 0; i < blocks.Count; i++, x++)
                    {
                        if (x >= numBlocksX)
                        {
                            x = 0;
                            y++;
                        }
                        blocks[i].Draw(bmpAddress, bmpWidth, bmpHeight, x * 16, y * 16);
                    }
                    for (; x < numBlocksX; x++)
                    {
                        RenderUtil.DrawCrossUnchecked(bmpAddress, bmpWidth, x * 16, y * 16, 16, 16, 0xFFFF0000);
                    }
                }
                if (collectionChanged)
                {
                    _isSelecting = false;
                    _selection.Start(0, 0, 1, 1);
                    FireSelectionCompleted();
                }
                InvalidateVisual();
            }
        }
    }
}
