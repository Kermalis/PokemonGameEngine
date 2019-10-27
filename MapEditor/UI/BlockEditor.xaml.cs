using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

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

        public ReactiveCommand AddBlockCommand { get; }
        public ReactiveCommand ClearBlockCommand { get; }
        public ReactiveCommand RemoveBlockCommand { get; }

        private readonly WriteableBitmap _selectionBitmap;
        private readonly Image _selectionImage;

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

        public BlockEditor()
        {
            AddBlockCommand = ReactiveCommand.Create(AddBlock);
            ClearBlockCommand = ReactiveCommand.Create(ClearBlock);
            RemoveBlockCommand = ReactiveCommand.Create(RemoveBlock);

            _tileset = Tileset.LoadOrGet("TestTiles");
            _blockset = Blockset.LoadOrGet("TestBlockset");
            _blockset.OnChanged += Blockset_OnChanged;

            _selectionBitmap = new WriteableBitmap(new PixelSize(8, 8), new Vector(96, 96), PixelFormat.Bgra8888);

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

            _selectionImage = this.FindControl<Image>("SelectionImage");
            _selectionImage.Source = _selectionBitmap;

            _tileLayerImage = this.FindControl<TileLayerImage>("TileLayerImage");
            _tileLayerImage.Selection.PropertyChanged += Selection_PropertyChanged;

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

        private void Blockset_OnChanged(Blockset blockset, Blockset.Block block)
        {
            if (block == _selectedBlock)
            {
                _tileLayerImage.UpdateBitmap();
                CountSubLayers();
                for (int i = 0; i < SubLayers.Count; i++)
                {
                    SubLayers[i].UpdateBitmap();
                }
                UpdateComboBox(_subLayerComboBox);
                for (int i = 0; i < ZLayers.Length; i++)
                {
                    ZLayers[i].UpdateBitmap();
                }
                UpdateComboBox(_zLayerComboBox);
            }
        }
        private unsafe void Selection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            using (ILockedFramebuffer l = _selectionBitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtil.TransparencyGrid(bmpAddress, 8, 8, 4, 4);
                _tileLayerImage.Selection.Draw(bmpAddress, 8, 8, 0, 0);
            }
            _selectionImage.InvalidateVisual();
        }
        private void TilesetImage_SelectionCompleted(object sender, Tileset.Tile e)
        {
            if (e != null)
            {
                _tileLayerImage.Selection.TilesetTile = e;
            }
        }
        private void BlocksetImage_SelectionCompleted(object sender, Blockset.Block[][] e)
        {
            Blockset.Block block = e[0][0];
            if (block != null && block != _selectedBlock)
            {
                _selectedBlock = block;
                _tileLayerImage.SetBlock(_selectedBlock);
                CountSubLayers();
                for (int i = 0; i < SubLayers.Count; i++)
                {
                    SubLayers[i].SetBlock(_selectedBlock);
                }
                UpdateComboBox(_subLayerComboBox);
                for (int i = 0; i < ZLayers.Length; i++)
                {
                    ZLayers[i].SetBlock(_selectedBlock);
                }
                UpdateComboBox(_zLayerComboBox);
            }
        }

        private void SetSubLayer(byte s)
        {
            _tileLayerImage.SetSubLayer(s);
        }
        private void SetZLayer(byte z)
        {
            _tileLayerImage.SetZLayer(z);
            CountSubLayers();
            for (int i = 0; i < SubLayers.Count; i++)
            {
                SubLayers[i].SetZLayer(z);
            }
            UpdateComboBox(_subLayerComboBox);
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
                        var s = new SubLayerModel(_selectedBlock, z, (byte)(curCount + i));
                        SubLayers.Add(s);
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
                        int index = curCount - 1 - i;
                        SubLayers[index].Dispose();
                        SubLayers.RemoveAt(index);
                    }
                }
            }
        }
        private void UpdateComboBox(ComboBox c)
        {
            // This forces a redraw
            IBrush old = c.Background;
            c.Background = old.Equals(Brushes.AliceBlue) ? Brushes.AntiqueWhite : Brushes.AliceBlue;
            c.Background = old;
        }

        protected override void HandleClosed()
        {
            _blockset.Save();
            Dispose();
            base.HandleClosed();
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
            _selectionBitmap.Dispose();
            _tileLayerImage.Dispose();
            AddBlockCommand.Dispose();
            ClearBlockCommand.Dispose();
            RemoveBlockCommand.Dispose();
        }
    }
}
