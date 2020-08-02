using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.UI.Models;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.Overworld;
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
            Clipboard = new Blockset.Block.Tile[OverworldConstants.Block_NumTilesY][];
            for (int y = 0; y < OverworldConstants.Block_NumTilesY; y++)
            {
                var arrY = new Blockset.Block.Tile[OverworldConstants.Block_NumTilesX];
                for (int x = 0; x < OverworldConstants.Block_NumTilesX; x++)
                {
                    arrY[x] = new Blockset.Block.Tile();
                }
                Clipboard[y] = arrY;
            }
            _selection = new Selection(OverworldConstants.Block_NumTilesX, OverworldConstants.Block_NumTilesY);
            _selection.Changed += OnSelectionChanged;
            _bitmap = new WriteableBitmap(new PixelSize(OverworldConstants.Block_NumPixelsX, OverworldConstants.Block_NumPixelsY), new Vector(96, 96), PixelFormat.Bgra8888);
            _bitmapSize = new Size(OverworldConstants.Block_NumPixelsX, OverworldConstants.Block_NumPixelsY);
        }

        public override void Render(DrawingContext context)
        {
            var viewPort = new Rect(Bounds.Size);
            Rect destRect = viewPort.CenterRect(new Rect(_bitmapSize * _scale)).Intersect(viewPort);
            Rect sourceRect = new Rect(_bitmapSize).CenterRect(new Rect(destRect.Size / _scale));

            context.DrawImage(_bitmap, 1, sourceRect, destRect);
            if (_isSelecting)
            {
                var r = new Rect(_selection.X * OverworldConstants.Tile_NumPixelsX * _scale, _selection.Y * OverworldConstants.Tile_NumPixelsY * _scale, _selection.Width * OverworldConstants.Tile_NumPixelsX * _scale, _selection.Height * OverworldConstants.Tile_NumPixelsY * _scale);
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
                List<Blockset.Block.Tile> subLayers = dict[_eLayerNum];
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
            for (int y = 0; y < OverworldConstants.Block_NumTilesY; y++)
            {
                int curY = startY + y;
                if (curY < OverworldConstants.Block_NumTilesY)
                {
                    Blockset.Block.Tile[] arrY = Clipboard[y];
                    for (int x = 0; x < OverworldConstants.Block_NumTilesX; x++)
                    {
                        int curX = startX + x;
                        if (curX < OverworldConstants.Block_NumTilesX)
                        {
                            Blockset.Block.Tile st = arrY[x];
                            if (st.TilesetTile != null)
                            {
                                Set(_block.Tiles[curY][curX], st);
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
            List<Blockset.Block.Tile> subLayers = _block.Tiles[y][x][_eLayerNum];
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
                        SetTiles((int)(pos.X / _scale) / OverworldConstants.Tile_NumPixelsX, (int)(pos.Y / _scale) / OverworldConstants.Tile_NumPixelsY);
                        e.Handled = true;
                    }
                    break;
                }
                case PointerUpdateKind.MiddleButtonPressed:
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        RemoveTile((int)(pos.X / _scale) / OverworldConstants.Tile_NumPixelsX, (int)(pos.Y / _scale) / OverworldConstants.Tile_NumPixelsY);
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
                        _selection.Start((int)(pos.X / _scale) / OverworldConstants.Tile_NumPixelsX, (int)(pos.Y / _scale) / OverworldConstants.Tile_NumPixelsY, 1, 1);
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
                        int x = (int)(pos.X / _scale) / OverworldConstants.Tile_NumPixelsX;
                        int y = (int)(pos.Y / _scale) / OverworldConstants.Tile_NumPixelsY;
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
                        for (int y = 0; y < OverworldConstants.Block_NumTilesY; y++)
                        {
                            Blockset.Block.Tile[] arrY = Clipboard[y];
                            for (int x = 0; x < OverworldConstants.Block_NumTilesX; x++)
                            {
                                Blockset.Block.Tile t = arrY[x];
                                if (x < width && y < height)
                                {
                                    Blockset.Block.Tile got = SubLayerModel.GetTile(_block, _eLayerNum, _subLayerNum, startX + x, startY + y);
                                    if (got != null)
                                    {
                                        if (!got.Equals(t))
                                        {
                                            changed = true;
                                            got.CopyTo(t);
                                        }
                                        continue;
                                    }
                                }
                                if (t.TilesetTile != null)
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
