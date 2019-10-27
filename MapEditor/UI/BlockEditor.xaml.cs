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
        private readonly ComboBox _zLayerComboBox;

        public ReactiveCommand AddBlockCommand { get; }
        public ReactiveCommand ClearBlockCommand { get; }
        public ReactiveCommand RemoveBlockCommand { get; }

        private readonly WriteableBitmap _selectionBitmap;
        private readonly Image _selectionImage;

        public ZLayerModel[] ZLayers { get; }
        private int _selectedZLayerIndex;
        public int SelectedZLayerIndex
        {
            get => _selectedZLayerIndex;
            set
            {
                if (value != -1 && _selectedZLayerIndex != value)
                {
                    _selectedZLayerIndex = value;
                    OnPropertyChanged(nameof(SelectedZLayerIndex));
                    _tileLayerImage.SetZLayer((byte)_selectedZLayerIndex);
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
            _blockset.OnReplaced += Blockset_OnReplaced;

            _selectionBitmap = new WriteableBitmap(new PixelSize(8, 8), new Vector(96, 96), PixelFormat.Bgra8888);

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
            _blockset.Add(_tileset.Tiles[0]);
        }
        private void ClearBlock()
        {
            Blockset.Replace(_selectedBlock, _tileset.Tiles[0]);
        }
        private void RemoveBlock()
        {
            if (_selectedBlock.Parent.Blocks.Count == 1)
            {
                Blockset.Replace(_selectedBlock, _tileset.Tiles[0]);
            }
            else
            {
                Blockset.Remove(_selectedBlock);
            }
        }
        private void UpdateZLayerComboBox()
        {
            // This forces a redraw
            IBrush old = _zLayerComboBox.Background;
            _zLayerComboBox.Background = old.Equals(Brushes.AliceBlue) ? Brushes.AntiqueWhite : Brushes.AliceBlue;
            _zLayerComboBox.Background = old;
        }

        private void Blockset_OnChanged(Blockset blockset, Blockset.Block block)
        {
            if (block == _selectedBlock)
            {
                for (int i = 0; i < ZLayers.Length; i++)
                {
                    ZLayers[i].UpdateBitmap();
                }
                UpdateZLayerComboBox();
            }
        }
        private void Blockset_OnReplaced(Blockset blockset, Blockset.Block oldBlock, Blockset.Block newBlock)
        {
            if (oldBlock == _selectedBlock)
            {
                SetBlock(newBlock);
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
                SetBlock(block);
            }
        }

        private void SetBlock(Blockset.Block block)
        {
            _selectedBlock = block;
            _tileLayerImage.SetBlock(block);
            for (int i = 0; i < ZLayers.Length; i++)
            {
                ZLayers[i].SetBlock(block);
            }
            UpdateZLayerComboBox();
        }

        protected override void HandleClosed()
        {
            _blockset.Save();
            Dispose();
            base.HandleClosed();
        }

        public void Dispose()
        {
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
