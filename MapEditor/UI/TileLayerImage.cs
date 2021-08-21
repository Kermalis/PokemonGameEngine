using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.UI.Models;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;

namespace Kermalis.MapEditor.UI
{
    public sealed class TileLayerImage : Control, IDisposable
    {
        internal readonly Blockset.Block.Tile[][] Clipboard;
        internal EventHandler ClipboardChanged;

        private bool _isDrawing;
        private bool _isSelecting;
        private readonly Selection _selection;

        private byte _subLayerNum;
        private byte _eLayerNum;
        private Blockset.Block _block;
        private readonly WriteableBitmap _bitmap;
        private readonly Size _bitmapSize;
        private readonly double _scale;

        public TileLayerImage(double scale)
        {
            _scale = scale;
            Clipboard = new Blockset.Block.Tile[Overworld.Block_NumTilesY][];
            for (int y = 0; y < Overworld.Block_NumTilesY; y++)
            {
                var arrY = new Blockset.Block.Tile[Overworld.Block_NumTilesX];
                for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                {
                    arrY[x] = new Blockset.Block.Tile();
                }
                Clipboard[y] = arrY;
            }
            _selection = new Selection(Overworld.Block_NumTilesX, Overworld.Block_NumTilesY);
            _selection.Changed += OnSelectionChanged;
            _bitmap = new WriteableBitmap(new PixelSize(Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Premul);
            _bitmapSize = new Size(Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY);
        }

        public override void Render(DrawingContext context)
        {
            var viewPort = new Rect(Bounds.Size);
            Rect destRect = viewPort.CenterRect(new Rect(_bitmapSize * _scale)).Intersect(viewPort);
            Rect sourceRect = new Rect(_bitmapSize).CenterRect(new Rect(destRect.Size / _scale));

            context.DrawImage(_bitmap, sourceRect, destRect);
            if (_isSelecting)
            {
                var r = new Rect(_selection.X * Overworld.Tile_NumPixelsX * _scale, _selection.Y * Overworld.Tile_NumPixelsY * _scale, _selection.Width * Overworld.Tile_NumPixelsX * _scale, _selection.Height * Overworld.Tile_NumPixelsY * _scale);
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
            void Set(List<Blockset.Block.Tile> subLayers, Blockset.Block.Tile st)
            {
                if (subLayers.Count <= _subLayerNum)
                {
                    changed = true;
                    var t = new Blockset.Block.Tile();
                    st.CopyTo(t);
                    subLayers.Add(t);
                }
                else
                {
                    Blockset.Block.Tile t = subLayers[_subLayerNum];
                    if (!st.Equals(t))
                    {
                        changed = true;
                        st.CopyTo(t);
                    }
                }
            }
            for (int y = 0; y < Overworld.Block_NumTilesY; y++)
            {
                int curY = startY + y;
                if (curY < Overworld.Block_NumTilesY)
                {
                    Blockset.Block.Tile[] arrY = Clipboard[y];
                    for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                    {
                        int curX = startX + x;
                        if (curX < Overworld.Block_NumTilesX)
                        {
                            Blockset.Block.Tile st = arrY[x];
                            if (st.TilesetTile is not null)
                            {
                                Set(_block.Tiles[_eLayerNum][curY][curX], st);
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
            List<Blockset.Block.Tile> subLayers = _block.Tiles[_eLayerNum][y][x];
            if (subLayers.Count > _subLayerNum)
            {
                subLayers.RemoveAt(_subLayerNum);
                _block.Parent.FireChanged(_block);
                UpdateBitmap();
            }
        }
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            PointerPoint pp = e.GetCurrentPoint(this);
            switch (pp.Properties.PointerUpdateKind)
            {
                case PointerUpdateKind.LeftButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        _isDrawing = true;
                        SetTiles((int)(pos.X / _scale) / Overworld.Tile_NumPixelsX, (int)(pos.Y / _scale) / Overworld.Tile_NumPixelsY);
                        e.Handled = true;
                    }
                    break;
                }
                case PointerUpdateKind.MiddleButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        RemoveTile((int)(pos.X / _scale) / Overworld.Tile_NumPixelsX, (int)(pos.Y / _scale) / Overworld.Tile_NumPixelsY);
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
                        _selection.Start((int)(pos.X / _scale) / Overworld.Tile_NumPixelsX, (int)(pos.Y / _scale) / Overworld.Tile_NumPixelsY, 1, 1);
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
                PointerPoint pp = e.GetCurrentPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        int x = (int)(pos.X / _scale) / Overworld.Tile_NumPixelsX;
                        int y = (int)(pos.Y / _scale) / Overworld.Tile_NumPixelsY;
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
                PointerPoint pp = e.GetCurrentPoint(this);
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
                        for (int y = 0; y < Overworld.Block_NumTilesY; y++)
                        {
                            Blockset.Block.Tile[] arrY = Clipboard[y];
                            for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                            {
                                Blockset.Block.Tile t = arrY[x];
                                if (x < width && y < height)
                                {
                                    Blockset.Block.Tile got = SubLayerModel.GetTile(_block, _eLayerNum, _subLayerNum, startX + x, startY + y);
                                    if (got is not null)
                                    {
                                        if (!got.Equals(t))
                                        {
                                            changed = true;
                                            got.CopyTo(t);
                                        }
                                        continue;
                                    }
                                }
                                if (t.TilesetTile is not null)
                                {
                                    changed = true;
                                }
                                t.TilesetTile = null;
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
            _block = block;
            UpdateBitmap();
        }
        internal void SetSubLayer(byte s)
        {
            if (_subLayerNum != s)
            {
                _subLayerNum = s;
                UpdateBitmap();
            }
        }
        internal void SetELayer(byte e)
        {
            if (_eLayerNum != e)
            {
                _eLayerNum = e;
                UpdateBitmap();
            }
        }
        internal unsafe void UpdateBitmap()
        {
            SubLayerModel.UpdateBitmap(_bitmap, _block, _eLayerNum, _subLayerNum);
            InvalidateVisual();
        }

        public void Dispose()
        {
            _bitmap.Dispose();
            _selection.Changed -= OnSelectionChanged;
            ClipboardChanged = null;
        }
    }
}
