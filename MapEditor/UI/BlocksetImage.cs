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

        private const Stretch _bitmapStretch = Stretch.None;
        private const int numBlocksX = 8;

        private bool _allowSelectingMultiple = true;
        public bool AllowSelectingMultiple
        {
            get => _allowSelectingMultiple;
            set
            {
                if (_allowSelectingMultiple != value)
                {
                    _allowSelectingMultiple = value;
                    ResetSelectionAndInvalidateVisual();
                    OnPropertyChanged(nameof(AllowSelectingMultiple));
                }
            }
        }

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
                    _blockset = value;
                    UpdateBlockset();
                    OnPropertyChanged(nameof(Blockset));
                }
            }
        }

        private bool _isSelecting;

        private WriteableBitmap _bitmap;
        private Size _bitmapSize;

        public BlocksetImage()
        {
            _selection = new Selection();
            _selection.Changed += OnSelectionChanged;

            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
        }

        private void ResetSelectionAndInvalidateVisual()
        {
            _isSelecting = false;
            _selection.Start(0, 0, 1, 1);
            InvalidateVisual();
        }

        private unsafe void UpdateBlockset()
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
                    // Draw an X for the unavailable ones
                    for (; x < numBlocksX; x++)
                    {
                        for (int py = 0; py < 16; py++)
                        {
                            for (int px = 0; px < 16; px++)
                            {
                                if (px == py)
                                {
                                    RenderUtil.Draw(bmpAddress + (x * 16) + px + (((y * 16) + py) * bmpWidth), 0xFFFF0000);
                                    RenderUtil.Draw(bmpAddress + (x * 16) + (15 - px) + (((y * 16) + py) * bmpWidth), 0xFFFF0000);
                                }
                            }
                        }
                    }
                }
            }
            ResetSelectionAndInvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            if (_blockset != null)
            {
                var viewPort = new Rect(Bounds.Size);
                Vector scale = _bitmapStretch.CalculateScaling(Bounds.Size, _bitmapSize);
                Size scaledSize = _bitmapSize * scale;
                Rect destRect = viewPort.CenterRect(new Rect(scaledSize)).Intersect(viewPort);
                Rect sourceRect = new Rect(_bitmapSize).CenterRect(new Rect(destRect.Size / scale));

                context.DrawImage(_bitmap, 1, sourceRect, destRect);
                var r = new Rect(_selection.X * 16, _selection.Y * 16, _selection.Width * 16, _selection.Height * 16);
                context.FillRectangle(_isSelecting ? Selection.SelectingBrush : Selection.SelectionBrush, r);
                context.DrawRectangle(_isSelecting ? Selection.SelectingPen : Selection.SelectionPen, r);
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_blockset != null)
            {
                if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
                {
                    return _bitmapSize;
                }
                else
                {
                    return _bitmapStretch.CalculateSize(availableSize, _bitmapSize);
                }
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_blockset != null)
            {
                return _bitmapStretch.CalculateSize(finalSize, _bitmapSize);
            }
            return new Size();
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (_blockset != null)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _isSelecting = _allowSelectingMultiple;
                        _selection.Start((int)pos.X / 16, (int)pos.Y / 16, 1, 1);
                        e.Handled = true;
                    }
                }
            }
        }
        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            if (_blockset != null && _isSelecting)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _selection.Move((int)pos.X / 16, (int)pos.Y / 16);
                        e.Handled = true;
                    }
                }
            }
        }
        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_blockset != null && _isSelecting)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                {
                    _isSelecting = false;
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
                    InvalidateVisual();
                    e.Handled = true;
                }
            }
        }
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
        }
    }
}
