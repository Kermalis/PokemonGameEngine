using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Kermalis.MapEditor.Core;
using System;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class ConnectionModel : INotifyPropertyChanged, IDisposable
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private Point _position;
        public Point Position
        {
            get => _position;
            internal set
            {
                if (!_position.Equals(value))
                {
                    _position = value;
                    OnPropertyChanged(nameof(Position));
                }
            }
        }
        private IBitmap _bitmap;
        public IBitmap Bitmap
        {
            get => _bitmap;
            private set
            {
                if (_bitmap != value)
                {
                    _bitmap = value;
                    OnPropertyChanged(nameof(Bitmap));
                }
            }
        }
        private IBrush _borderBrush;
        public IBrush BorderBrush
        {
            get => _borderBrush;
            private set
            {
                if (_borderBrush?.Equals(value) != true)
                {
                    _borderBrush = value;
                    OnPropertyChanged(nameof(BorderBrush));
                }
            }
        }
        private double _borderThickness;
        public double BorderThickness
        {
            get => _borderThickness;
            private set
            {
                if (_borderThickness != value)
                {
                    _borderThickness = value;
                    OnPropertyChanged(nameof(BorderThickness));
                }
            }
        }
        private IBrush _opacityMask;
        public IBrush OpacityMask
        {
            get => _opacityMask;
            private set
            {
                if (_opacityMask?.Equals(value) != true)
                {
                    _opacityMask = value;
                    OnPropertyChanged(nameof(OpacityMask));
                }
            }
        }

        private double _width;
        public double Width
        {
            get => _width;
            private set
            {
                if (_width != value)
                {
                    _width = value;
                    OnPropertyChanged(nameof(Width));
                }
            }
        }
        private double _height;
        public double Height
        {
            get => _height;
            private set
            {
                if (_height != value)
                {
                    _height = value;
                    OnPropertyChanged(nameof(Height));
                }
            }
        }
        internal Map Map { get; private set; }

        internal ConnectionModel(Map map, bool transparentMask)
        {
            _opacityMask = GetOpacityMask(transparentMask);
            SetMap(map);
        }

        internal void SetMap(Map map)
        {
            Map.Layout ml = map.MapLayout;
            Width = ml.Width * 16;
            Height = ml.Height * 16;
            Map = map;
            Bitmap = Map.MapLayout.BlocksBitmap;
        }

        internal void Select(bool s)
        {
            BorderBrush = s ? Brushes.Red : null;
            BorderThickness = s ? 1 : 0;
            OpacityMask = GetOpacityMask(s);
        }

        private static IBrush _opacityBrush;
        internal static void CreateBrush()
        {
            _opacityBrush = new SolidColorBrush(0x40000000);
        }
        private IBrush GetOpacityMask(bool s)
        {
            return s ? null : _opacityBrush;
        }

        public void Dispose()
        {
            PropertyChanged = null;
        }
    }
}
