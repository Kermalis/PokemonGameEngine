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
    public sealed class BlockEditor : Window, IDisposable, INotifyPropertyChanged
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
        private readonly ComboBox _zLayerComboBox;

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
        public ZLayerModel[] ZLayers { get; }
        private int _selectedZLayerIndex = -1;
        public int SelectedZLayerIndex
        {
            get => _selectedZLayerIndex;
            set
            {
                if (value != -1 && _selectedZLayerIndex != value)
                {
                    _selectedZLayerIndex = value;
                    OnPropertyChanged(nameof(SelectedZLayerIndex));
                    SetZLayer((byte)_selectedZLayerIndex);
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
        public int ClipboardBorderWidth { get; private set; }
        public int ClipboardBorderHeight { get; private set; }

        public BlockEditor()
        {
            AddBlockCommand = ReactiveCommand.Create(AddBlock);
            ClearBlockCommand = ReactiveCommand.Create(ClearBlock);
            RemoveBlockCommand = ReactiveCommand.Create(RemoveBlock);

            _tileset = Tileset.LoadOrGet("TestTiles");
            _blockset = Blockset.LoadOrGet("TestBlockset");
            _blockset.OnChanged += Blockset_OnChanged;

            _clipboardBitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888);

            SubLayers = new ObservableCollection<SubLayerModel>(new List<SubLayerModel>(byte.MaxValue + 1));

            ZLayers = new ZLayerModel[byte.MaxValue + 1];
            byte z = 0;
            while (true)
            {
                ZLayers[z] = new ZLayerModel(z);
                if (z == byte.MaxValue)
                {
                    break;
                }
                z++;
            }

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _subLayerComboBox = this.FindControl<ComboBox>("SubLayerComboBox");
            _zLayerComboBox = this.FindControl<ComboBox>("ZLayerComboBox");

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
                for (int y = 0; y < tli.ClipboardHeight; y++)
                {
                    Blockset.Block.Tile[] arrY = c[y];
                    for (int x = 0; x < tli.ClipboardWidth; x++)
                    {
                        Blockset.Block.Tile t = arrY[x];
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
                DrawClipboard();
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
                count = ZLayers.Length;
                for (int i = 0; i < count; i++)
                {
                    ZLayers[i].UpdateBitmap();
                }
                _zLayerComboBox.ForceRedraw();
            }
        }
        private void TileLayerImage_ClipboardChanged(object sender, EventArgs e)
        {
            TileLayerImage tli = _tileLayerImage;
            Blockset.Block.Tile[][] c = tli.Clipboard;
            bool? xf = null;
            bool? yf = null;
            for (int y = 0; y < tli.ClipboardHeight; y++)
            {
                Blockset.Block.Tile[] arrY = c[y];
                for (int x = 0; x < tli.ClipboardWidth; x++)
                {
                    Blockset.Block.Tile t = arrY[x];
                    bool txf = t.XFlip;
                    bool tyf = t.YFlip;
                    if (x == 0 && y == 0)
                    {
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
            _ignoreFlipChange = true;
            XFlip = xf;
            YFlip = yf;
            _ignoreFlipChange = false;
            UpdateClipboardBorders();
            DrawClipboard();
        }
        private void TilesetImage_SelectionCompleted(object sender, Tileset.Tile[][] e)
        {
            if (e != null)
            {
                TileLayerImage tli = _tileLayerImage;
                Blockset.Block.Tile[][] c = tli.Clipboard;
                int el = e.Length;
                tli.ClipboardHeight = el;
                bool xV = _xFlip.HasValue;
                bool yV = _yFlip.HasValue;
                for (int y = 0; y < el; y++)
                {
                    Blockset.Block.Tile[] sy = c[y];
                    Tileset.Tile[] ey = e[y];
                    int eyl = ey.Length;
                    tli.ClipboardWidth = eyl;
                    for (int x = 0; x < eyl; x++)
                    {
                        Blockset.Block.Tile t = sy[x];
                        t.TilesetTile = ey[x];
                        if (xV)
                        {
                            t.XFlip = _xFlip.Value;
                        }
                        if (yV)
                        {
                            t.YFlip = _yFlip.Value;
                        }
                    }
                }
                UpdateClipboardBorders();
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
                for (int i = 0; i < ZLayers.Length; i++)
                {
                    ZLayers[i].SetBlock(block);
                }
                _zLayerComboBox.ForceRedraw();
            }
        }
        private unsafe void DrawClipboard()
        {
            using (ILockedFramebuffer l = _clipboardBitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtil.TransparencyGrid(bmpAddress, 16, 16, 4, 4);
                TileLayerImage tli = _tileLayerImage;
                Blockset.Block.Tile[][] c = tli.Clipboard;
                for (int y = 0; y < tli.ClipboardHeight; y++)
                {
                    Blockset.Block.Tile[] arrY = c[y];
                    for (int x = 0; x < tli.ClipboardWidth; x++)
                    {
                        arrY[x].Draw(bmpAddress, 16, 16, x * 8, y * 8);
                    }
                }
            }
            _clipboardImage.InvalidateVisual();
        }

        private void SetSubLayer(byte s)
        {
            _tileLayerImage.SetSubLayer(s);
        }
        private void SetZLayer(byte z)
        {
            _tileLayerImage.SetZLayer(z);
            CountSubLayers();
            int count = SubLayers.Count;
            for (int i = 0; i < count; i++)
            {
                SubLayers[i].SetZLayer(z);
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
            if (_selectedZLayerIndex == -1)
            {
                SelectedZLayerIndex = 0;
            }
            byte z = (byte)_selectedZLayerIndex;
            Count(_selectedBlock.TopLeft[z]);
            Count(_selectedBlock.TopRight[z]);
            Count(_selectedBlock.BottomLeft[z]);
            Count(_selectedBlock.BottomRight[z]);
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
                        SubLayers.Add(new SubLayerModel(_selectedBlock, z, (byte)(curCount + i)));
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
        private void UpdateClipboardBorders()
        {
            TileLayerImage tli = _tileLayerImage;
            ClipboardBorderHeight = (tli.ClipboardHeight * 8 * 2) + 2;
            OnPropertyChanged(nameof(ClipboardBorderHeight));
            ClipboardBorderWidth = (tli.ClipboardWidth * 8 * 2) + 2;
            OnPropertyChanged(nameof(ClipboardBorderWidth));
        }

        protected override bool HandleClosing()
        {
            _blockset.Save();
            Dispose();
            return base.HandleClosing();
        }

        public void Dispose()
        {
            for (int i = 0; i < SubLayers.Count; i++)
            {
                SubLayers[i].Dispose();
            }
            for (int i = 0; i < ZLayers.Length; i++)
            {
                ZLayers[i].Dispose();
            }
            _clipboardBitmap.Dispose();
            _tileLayerImage.Dispose();
            AddBlockCommand.Dispose();
            ClearBlockCommand.Dispose();
            RemoveBlockCommand.Dispose();
        }
    }
}
