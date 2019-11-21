using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;

namespace Kermalis.MapEditor.UI
{
    public sealed class BlocksetEditor : Window, IDisposable, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

#pragma warning disable IDE0069 // Disposable fields should be disposed
        private readonly Tileset _tileset;
        private readonly Blockset _blockset;
#pragma warning restore IDE0069 // Disposable fields should be disposed
        private readonly TileLayerImage _tileLayerImage;
        private readonly TilesetImage _tilesetImage;
        private readonly BlocksetImage _blocksetImage;
        private readonly ComboBox _subLayerComboBox;
        private readonly ComboBox _eLayerComboBox;

        public ReactiveCommand<Unit, Unit> SaveBlocksetCommand { get; }
        public ReactiveCommand<Unit, Unit> AddBlockCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearBlockCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveBlockCommand { get; }

        private readonly WriteableBitmap _clipboardBitmap;
        private readonly Image _clipboardImage;

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
                    SetSubLayer((byte)_selectedSubLayerIndex);
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
                    SetELayer((byte)_selectedELayerIndex);
                }
            }
        }
        private Blockset.Block _selectedBlock;
        private bool _ignoreFlipChange = false;
        private bool? _xFlip = false;
        public bool? XFlip
        {
            get => _xFlip;
            set
            {
                if (_xFlip?.Equals(value) != true)
                {
                    _xFlip = value;
                    if (!_ignoreFlipChange)
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
                    if (!_ignoreFlipChange)
                    {
                        OnFlipChanged(false);
                    }
                    OnPropertyChanged(nameof(YFlip));
                }
            }
        }

        public BlocksetEditor()
        {
            SaveBlocksetCommand = ReactiveCommand.Create(SaveBlockset);
            AddBlockCommand = ReactiveCommand.Create(AddBlock);
            ClearBlockCommand = ReactiveCommand.Create(ClearBlock);
            RemoveBlockCommand = ReactiveCommand.Create(RemoveBlock);

            _tileset = Tileset.LoadOrGet("TestTilesO");
            _blockset = Blockset.LoadOrGet("TestBlocksetO");
            _blockset.OnChanged += Blockset_OnChanged;

            _clipboardBitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888);

            SubLayers = new ObservableCollection<SubLayerModel>(new List<SubLayerModel>(byte.MaxValue + 1));

            ELayers = new ELayerModel[byte.MaxValue + 1];
            byte e = 0;
            while (true)
            {
                ELayers[e] = new ELayerModel(e);
                if (e == byte.MaxValue)
                {
                    break;
                }
                e++;
            }

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _subLayerComboBox = this.FindControl<ComboBox>("SubLayerComboBox");
            _eLayerComboBox = this.FindControl<ComboBox>("ELayerComboBox");

            _clipboardImage = this.FindControl<Image>("ClipboardImage");
            _clipboardImage.Source = _clipboardBitmap;

            _tileLayerImage = this.FindControl<TileLayerImage>("TileLayerImage");
            _tileLayerImage.ClipboardChanged += TileLayerImage_ClipboardChanged;

            _tilesetImage = this.FindControl<TilesetImage>("TilesetImage");
            _tilesetImage.SelectionCompleted += TilesetImage_SelectionCompleted;
            _tilesetImage.Tileset = _tileset;

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksetImage");
            _blocksetImage.SelectionCompleted += BlocksetImage_SelectionCompleted;
            _blocksetImage.Blockset = _blockset;
        }

        private void SaveBlockset()
        {
            _blockset.Save();
        }
        private void AddBlock()
        {
            _blockset.Add();
        }
        private void ClearBlock()
        {
            Blockset.Clear(_selectedBlock);
        }
        private void RemoveBlock()
        {
            if (_selectedBlock.Parent.Blocks.Count == 1)
            {
                Blockset.Clear(_selectedBlock);
            }
            else
            {
                Blockset.Remove(_selectedBlock);
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
                for (int y = 0; y < 2; y++)
                {
                    Blockset.Block.Tile[] arrY = c[y];
                    for (int x = 0; x < 2; x++)
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
            for (int y = 0; y < 2; y++)
            {
                Blockset.Block.Tile[] arrY = c[y];
                for (int x = 0; x < 2; x++)
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
            _ignoreFlipChange = true;
            XFlip = xf;
            YFlip = yf;
            _ignoreFlipChange = false;
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
                for (int y = 0; y < 2; y++)
                {
                    Blockset.Block.Tile[] cy = c[y];
                    for (int x = 0; x < 2; x++)
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
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtil.ClearUnchecked(bmpAddress, 16, 0, 0, 16, 16);
                TileLayerImage tli = _tileLayerImage;
                Blockset.Block.Tile[][] c = tli.Clipboard;
                for (int y = 0; y < 2; y++)
                {
                    int ty = y * 8;
                    Blockset.Block.Tile[] arrY = c[y];
                    for (int x = 0; x < 2; x++)
                    {
                        Blockset.Block.Tile t = arrY[x];
                        if (t.TilesetTile != null)
                        {
                            int tx = x * 8;
                            RenderUtil.TransparencyGrid(bmpAddress, 16, 16, tx, ty, 4, 4, 2, 2);
                            t.Draw(bmpAddress, 16, 16, tx, ty);
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
            Count(_selectedBlock.TopLeft[e]);
            Count(_selectedBlock.TopRight[e]);
            Count(_selectedBlock.BottomLeft[e]);
            Count(_selectedBlock.BottomRight[e]);
            if (num < byte.MaxValue + 1)
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
            _blockset.OnChanged -= Blockset_OnChanged;
        }
        public void Dispose()
        {
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
            _tileLayerImage.ClipboardChanged -= TileLayerImage_ClipboardChanged;
            _tilesetImage.Dispose();
            _tilesetImage.SelectionCompleted -= TilesetImage_SelectionCompleted;
            _blocksetImage.Dispose();
            _blocksetImage.SelectionCompleted -= BlocksetImage_SelectionCompleted;
            SaveBlocksetCommand.Dispose();
            AddBlockCommand.Dispose();
            ClearBlockCommand.Dispose();
            RemoveBlockCommand.Dispose();
        }
    }
}
