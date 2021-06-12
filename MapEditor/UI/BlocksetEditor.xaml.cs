﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.UI.Models;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class BlocksetEditor : Window, IDisposable, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private Blockset _blockset;
        private readonly TileLayerImage _tileLayerImage;
        private readonly TilesetImage _tilesetImage;
        private readonly BlocksetImage _blocksetImage;
        private readonly ComboBox _subLayerComboBox;
        private readonly ComboBox _eLayerComboBox;

        private readonly WriteableBitmap _clipboardBitmap;
        private readonly Image _clipboardImage;

        private bool _ignoreChange = false;

        public static IEnumerable<BlocksetBlockBehavior> Behaviors { get; } = Utils.GetEnumValues<BlocksetBlockBehavior>();
        private BlocksetBlockBehavior _selectedBehavior;
        public BlocksetBlockBehavior SelectedBehavior
        {
            get => _selectedBehavior;
            set
            {
                if (_selectedBehavior != value)
                {
                    _selectedBehavior = value;
                    if (!_ignoreChange)
                    {
                        _selectedBlock.Behavior = value;
                    }
                    OnPropertyChanged(nameof(SelectedBehavior));
                }
            }
        }

        private string _selectedBlockset;
        public string SelectedBlockset
        {
            get => _selectedBlockset;
            set
            {
                if (_selectedBlockset != value)
                {
                    _selectedBlockset = value;
                    RemoveBlocksetEvents();
                    _blockset = Blockset.LoadOrGet(value);
                    _blockset.OnChanged += Blockset_OnChanged;
                    _blocksetImage.Blockset = _blockset;
                    OnPropertyChanged(nameof(SelectedBlockset));
                }
            }
        }
        private string _selectedTileset;
        public string SelectedTileset
        {
            get => _selectedTileset;
            set
            {
                if (_selectedTileset != value)
                {
                    _selectedTileset = value;
                    _tilesetImage.Tileset = Tileset.LoadOrGet(value);
                    OnPropertyChanged(nameof(SelectedTileset));
                }
            }
        }

        public ObservableCollection<SubLayerModel> SubLayers { get; }
        private int _selectedSubLayerIndex = -1;
        public int SelectedSubLayerIndex
        {
            get => _selectedSubLayerIndex;
            set
            {
                if (value != -1 && _selectedSubLayerIndex != value)
                {
                    _selectedSubLayerIndex = value;
                    OnPropertyChanged(nameof(SelectedSubLayerIndex));
                    SetSubLayer((byte)value);
                }
            }
        }
        public ELayerModel[] ELayers { get; }
        private int _selectedELayerIndex = -1;
        public int SelectedELayerIndex
        {
            get => _selectedELayerIndex;
            set
            {
                if (value != -1 && _selectedELayerIndex != value)
                {
                    _selectedELayerIndex = value;
                    OnPropertyChanged(nameof(SelectedELayerIndex));
                    SetELayer((byte)value);
                }
            }
        }
        private Blockset.Block _selectedBlock;
        private bool? _xFlip = false;
        public bool? XFlip
        {
            get => _xFlip;
            set
            {
                if (_xFlip?.Equals(value) != true)
                {
                    _xFlip = value;
                    if (!_ignoreChange)
                    {
                        OnFlipChanged(true);
                    }
                    OnPropertyChanged(nameof(XFlip));
                }
            }
        }
        private bool? _yFlip = false;
        public bool? YFlip
        {
            get => _yFlip;
            set
            {
                if (_yFlip?.Equals(value) != true)
                {
                    _yFlip = value;
                    if (!_ignoreChange)
                    {
                        OnFlipChanged(false);
                    }
                    OnPropertyChanged(nameof(YFlip));
                }
            }
        }

        public BlocksetEditor()
        {
            _clipboardBitmap = new WriteableBitmap(new PixelSize(Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Premul);

            SubLayers = new ObservableCollection<SubLayerModel>(new List<SubLayerModel>(Overworld.MaxSubLayers));

            ELayers = new ELayerModel[Overworld.NumElevations];
            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                ELayers[e] = new ELayerModel(e);
            }

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _subLayerComboBox = this.FindControl<ComboBox>("SubLayerComboBox");
            _eLayerComboBox = this.FindControl<ComboBox>("ELayerComboBox");

            _clipboardImage = this.FindControl<Image>("ClipboardImage");
            _clipboardImage.Width = Overworld.Block_NumPixelsX * 2;
            _clipboardImage.Height = Overworld.Block_NumPixelsY * 2;
            _clipboardImage.Source = _clipboardBitmap;

            _tileLayerImage = this.FindControl<TileLayerImage>("TileLayerImage");
            _tileLayerImage.ClipboardChanged += TileLayerImage_ClipboardChanged;

            _tilesetImage = this.FindControl<TilesetImage>("TilesetImage");
            _tilesetImage.SelectionCompleted += TilesetImage_SelectionCompleted;

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            SelectedBlockset = Blockset.Ids[0];
            SelectedTileset = Tileset.Ids[0];
        }

        public void SaveBlockset()
        {
            _blockset.Save();
        }
        public void AddBlock()
        {
            _blockset.Add();
        }
        public void ClearBlock()
        {
            Blockset.Clear(_selectedBlock);
        }
        public void RemoveBlock()
        {
            Blockset.Block b = _selectedBlock;
            Blockset blockset = b.Parent;
            int oldCount = blockset.Blocks.Count;
            if (oldCount == 1)
            {
                Blockset.Clear(b);
            }
            else
            {
                int index = blockset.Blocks.IndexOf(b);
                Blockset.Remove(b);
                if (index == oldCount - 1)
                {
                    _blocksetImage.SelectBlock(oldCount - 2);
                }
            }
        }

        private void OnFlipChanged(bool xFlipChanged)
        {
            bool? nv = xFlipChanged ? _xFlip : _yFlip;
            if (nv.HasValue)
            {
                bool value = nv.Value;
                TileLayerImage tli = _tileLayerImage;
                Blockset.Block.Tile[][] c = tli.Clipboard;
                bool changed = false;
                for (int y = 0; y < Overworld.Block_NumTilesY; y++)
                {
                    Blockset.Block.Tile[] arrY = c[y];
                    for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                    {
                        Blockset.Block.Tile t = arrY[x];
                        if (t.TilesetTile != null)
                        {
                            changed = true;
                            if (xFlipChanged)
                            {
                                t.XFlip = value;
                            }
                            else
                            {
                                t.YFlip = value;
                            }
                        }
                    }
                }
                if (changed)
                {
                    DrawClipboard();
                }
            }
        }
        private void Blockset_OnChanged(Blockset blockset, Blockset.Block block)
        {
            if (block == _selectedBlock)
            {
                _tileLayerImage.UpdateBitmap();
                CountSubLayers();
                int count = SubLayers.Count;
                for (int i = 0; i < count; i++)
                {
                    SubLayers[i].UpdateBitmap();
                }
                _subLayerComboBox.ForceRedraw();
                count = ELayers.Length;
                for (int i = 0; i < count; i++)
                {
                    ELayers[i].UpdateBitmap();
                }
                _eLayerComboBox.ForceRedraw();
            }
        }
        private void TileLayerImage_ClipboardChanged(object sender, EventArgs e)
        {
            TileLayerImage tli = _tileLayerImage;
            Blockset.Block.Tile[][] c = tli.Clipboard;
            bool? xf = null;
            bool? yf = null;
            bool first = true;
            for (int y = 0; y < Overworld.Block_NumTilesY; y++)
            {
                Blockset.Block.Tile[] arrY = c[y];
                for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                {
                    Blockset.Block.Tile t = arrY[x];
                    if (t.TilesetTile != null)
                    {
                        bool txf = t.XFlip;
                        bool tyf = t.YFlip;
                        if (first)
                        {
                            first = false;
                            xf = txf;
                            yf = tyf;
                        }
                        else
                        {
                            if (xf?.Equals(txf) != true)
                            {
                                xf = null;
                            }
                            if (yf?.Equals(tyf) != true)
                            {
                                yf = null;
                            }
                        }
                    }
                }
            }
            _ignoreChange = true;
            XFlip = xf;
            YFlip = yf;
            _ignoreChange = false;
            DrawClipboard();
        }
        private void TilesetImage_SelectionCompleted(object sender, Tileset.Tile[][] e)
        {
            if (e != null)
            {
                TileLayerImage tli = _tileLayerImage;
                Blockset.Block.Tile[][] c = tli.Clipboard;
                bool xV = _xFlip.HasValue;
                bool yV = _yFlip.HasValue;
                for (int y = 0; y < Overworld.Block_NumTilesY; y++)
                {
                    Blockset.Block.Tile[] cy = c[y];
                    for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                    {
                        Blockset.Block.Tile t = cy[x];
                        if (y < e.Length)
                        {
                            Tileset.Tile[] ey = e[y];
                            if (x < ey.Length)
                            {
                                t.TilesetTile = e[y][x];
                                if (xV)
                                {
                                    t.XFlip = _xFlip.Value;
                                }
                                if (yV)
                                {
                                    t.YFlip = _yFlip.Value;
                                }
                                continue;
                            }
                        }
                        t.TilesetTile = null;
                    }
                }
                DrawClipboard();
            }
        }
        private void BlocksetImage_SelectionCompleted(object sender, Blockset.Block[][] e)
        {
            Blockset.Block block = e[0][0];
            if (block != null && block != _selectedBlock)
            {
                _selectedBlock = block;
                _ignoreChange = true;
                SelectedBehavior = block.Behavior;
                _ignoreChange = false;
                _tileLayerImage.SetBlock(block);
                CountSubLayers();
                for (int i = 0; i < SubLayers.Count; i++)
                {
                    SubLayers[i].SetBlock(block);
                }
                _subLayerComboBox.ForceRedraw();
                for (int i = 0; i < ELayers.Length; i++)
                {
                    ELayers[i].SetBlock(block);
                }
                _eLayerComboBox.ForceRedraw();
            }
        }
        private unsafe void DrawClipboard()
        {
            using (ILockedFramebuffer l = _clipboardBitmap.Lock())
            {
                uint* dst = (uint*)l.Address.ToPointer();
                Util.Renderer.ClearRectangle_Unchecked(dst, Overworld.Block_NumPixelsX, 0, 0, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY);
                TileLayerImage tli = _tileLayerImage;
                Blockset.Block.Tile[][] c = tli.Clipboard;
                for (int y = 0; y < Overworld.Block_NumTilesY; y++)
                {
                    int ty = y * Overworld.Tile_NumPixelsY;
                    Blockset.Block.Tile[] arrY = c[y];
                    for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                    {
                        Blockset.Block.Tile t = arrY[x];
                        if (t.TilesetTile != null)
                        {
                            int tx = x * Overworld.Tile_NumPixelsX;
                            Util.Renderer.TransparencyGrid(dst, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY, tx, ty, Overworld.Tile_NumPixelsX / 2, Overworld.Tile_NumPixelsY / 2, Overworld.Block_NumTilesX, Overworld.Block_NumTilesY);
                            t.Draw(dst, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY, tx, ty);
                        }
                    }
                }
            }
            _clipboardImage.InvalidateVisual();
        }

        private void SetSubLayer(byte s)
        {
            _tileLayerImage.SetSubLayer(s);
        }
        private void SetELayer(byte e)
        {
            _tileLayerImage.SetELayer(e);
            CountSubLayers();
            int count = SubLayers.Count;
            for (int i = 0; i < count; i++)
            {
                SubLayers[i].SetELayer(e);
            }
            _subLayerComboBox.ForceRedraw();
        }
        private void CountSubLayers()
        {
            int num = 0;
            void Count(List<Blockset.Block.Tile> subLayers)
            {
                int count = subLayers.Count;
                if (count > num)
                {
                    num = count;
                }
            }
            if (_selectedELayerIndex == -1)
            {
                SelectedELayerIndex = 0;
            }
            byte e = (byte)_selectedELayerIndex;
            List<Blockset.Block.Tile>[][] arrE = _selectedBlock.Tiles[e];
            for (int y = 0; y < Overworld.Block_NumTilesY; y++)
            {
                List<Blockset.Block.Tile>[] arrY = arrE[y];
                for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                {
                    Count(arrY[x]);
                }
            }
            if (num < Overworld.MaxSubLayers)
            {
                num++;
            }
            int curCount = SubLayers.Count;
            if (num != curCount)
            {
                if (num > curCount)
                {
                    int numToAdd = num - curCount;
                    for (int i = 0; i < numToAdd; i++)
                    {
                        SubLayers.Add(new SubLayerModel(_selectedBlock, e, (byte)(curCount + i)));
                    }
                    if (_selectedSubLayerIndex == -1)
                    {
                        SelectedSubLayerIndex = 0;
                    }
                }
                else
                {
                    int numToRemove = curCount - num;
                    for (int i = 0; i < numToRemove; i++)
                    {
                        SubLayerModel s = SubLayers[curCount - 1 - i];
                        s.Dispose();
                        SubLayers.Remove(s);
                    }
                    int count = SubLayers.Count;
                    if (_selectedSubLayerIndex >= count)
                    {
                        SelectedSubLayerIndex = count - 1;
                    }
                }
            }
        }

        protected override bool HandleClosing()
        {
            Dispose();
            return base.HandleClosing();
        }
        private void RemoveBlocksetEvents()
        {
            if (_blockset != null)
            {
                _blockset.OnChanged -= Blockset_OnChanged;
            }
        }
        public void Dispose()
        {
            PropertyChanged = null;
            RemoveBlocksetEvents();
            for (int i = 0; i < SubLayers.Count; i++)
            {
                SubLayers[i].Dispose();
            }
            for (int i = 0; i < ELayers.Length; i++)
            {
                ELayers[i].Dispose();
            }
            _clipboardBitmap.Dispose();
            _tileLayerImage.Dispose();
            _tilesetImage.Dispose();
            _blocksetImage.Dispose();
        }
    }
}
