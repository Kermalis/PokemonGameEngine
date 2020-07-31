using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.Overworld;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Kermalis.MapEditor.UI
{
    public sealed class MovementImage : Control, IDisposable, INotifyPropertyChanged, IValueConverter
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private bool _passageShown = true;
        public bool PassageShown
        {
            get => _passageShown;
            set
            {
                if (value != _passageShown)
                {
                    _passageShown = value;
                    OnPropertyChanged(nameof(PassageShown));
                    InvalidateVisual();
                }
            }
        }
        public byte _selectedElevation;
        public byte SelectedElevation
        {
            get => _selectedElevation;
            set
            {
                if (value != _selectedElevation)
                {
                    _selectedElevation = value;
                    OnPropertyChanged(nameof(SelectedElevation));
                }
            }
        }
        private LayoutBlockPassage _passage;
        public LayoutBlockPassage Passage
        {
            get => _passage;
            set
            {
                if (value != _passage)
                {
                    _passage = value;
                    OnPropertyChanged(nameof(Passage));
                }
            }
        }

        private Map.Layout _layout;
        internal Map.Layout Layout
        {
            get => _layout;
            set
            {
                if (_layout != value)
                {
                    RemoveLayoutEvents();
                    _layout = value;
                    value.OnDrew += MapLayout_OnDrew;
                    InvalidateMeasure();
                    InvalidateVisual();
                }
            }
        }

        private bool _isDrawing;
        private readonly SolidColorBrush _brush;
        private readonly Pen _pen;
        private readonly FormattedText _text;

        private static readonly Dictionary<byte, uint> _elevationColors;
        private static readonly Dictionary<LayoutBlockPassage, Geometry> _passageGeometries;
        static MovementImage()
        {
            var r = new Random(1114);
            _elevationColors = new Dictionary<byte, uint>(256);
            byte b = 0;
            while (true)
            {
                _elevationColors.Add(b, (uint)(0x80000000 + r.Next(0, 0x1000000)));
                if (b == byte.MaxValue)
                {
                    break;
                }
                b++;
            }
            const LayoutBlockPassage max = LayoutBlockPassage.SouthwestPassage | LayoutBlockPassage.SoutheastPassage | LayoutBlockPassage.NorthwestPassage | LayoutBlockPassage.NortheastPassage | LayoutBlockPassage.AllowOccupancy;
            _passageGeometries = new Dictionary<LayoutBlockPassage, Geometry>((byte)max + 1);
            for (LayoutBlockPassage i = 0; i <= max; i++)
            {
                string s;
                if (i.HasFlag(LayoutBlockPassage.AllowOccupancy))
                {
                    s = string.Empty;
                    if (!i.HasFlag(LayoutBlockPassage.SouthwestPassage))
                    {
                        s += "M 0,9 V 16 H 8 L 0,9";
                    }
                    if (!i.HasFlag(LayoutBlockPassage.SoutheastPassage))
                    {
                        s += "M 9,16 H 16 V 9 L 9,16";
                    }
                    if (!i.HasFlag(LayoutBlockPassage.NorthwestPassage))
                    {
                        s += "M 8,0 H 0 V 8 L 8,0";
                    }
                    if (!i.HasFlag(LayoutBlockPassage.NortheastPassage))
                    {
                        s += "M 16,8 V 0 H 9 L 16,8";
                    }
                }
                else
                {
                    s = (i.HasFlag(LayoutBlockPassage.SouthwestPassage) ? "M 0,9 L 8,16" : "M 0,16 H 8")
                    + (i.HasFlag(LayoutBlockPassage.SoutheastPassage) ? " H 9 L 16,9" : " H 16")
                    + (i.HasFlag(LayoutBlockPassage.NortheastPassage) ? " V 8 L 9,0" : " V 0")
                    + (i.HasFlag(LayoutBlockPassage.NorthwestPassage) ? " H 8 L 0,8" : " H 0")
                    + " Z";
                }
                var geo = Geometry.Parse(s);
                geo.Transform = new TranslateTransform();
                _passageGeometries.Add(i, geo);
            }
        }
        public MovementImage()
        {
            _brush = new SolidColorBrush();
            _pen = new Pen(_brush);
            _text = new FormattedText { Constraint = new Size(16, 16), TextAlignment = TextAlignment.Center, Typeface = Typeface.Default };
        }

        private void MapLayout_OnDrew(Map.Layout layout, bool drewBorderBlocks, bool wasResized)
        {
            if (!drewBorderBlocks)
            {
                if (wasResized)
                {
                    InvalidateMeasure();
                }
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context)
        {
            if (_layout != null)
            {
                IBitmap source = _layout.BlocksBitmap;
                var viewPort = new Rect(Bounds.Size);
                var r = new Rect(source.Size);
                Rect destRect = viewPort.CenterRect(r).Intersect(viewPort);
                Rect sourceRect = r.CenterRect(new Rect(destRect.Size));

                context.DrawImage(source, 1, sourceRect, destRect);

                for (int y = 0; y < _layout.Height; y++)
                {
                    int by = y * 16;
                    Map.Layout.Block[] arrY = _layout.Blocks[y];
                    for (int x = 0; x < _layout.Width; x++)
                    {
                        int bx = x * 16;
                        var r2 = new Rect(bx, by, 16, 16);
                        Map.Layout.Block b = arrY[x];
                        if (_passageShown)
                        {
                            _brush.Color = Color.FromUInt32(0x80008000);
                            context.FillRectangle(_brush, r2);
                            context.DrawRectangle(_pen, r2);
                            Geometry geo = _passageGeometries[b.Passage];
                            var tt = (TranslateTransform)geo.Transform;
                            tt.X = bx;
                            tt.Y = by;
                            _brush.Color = Color.FromUInt32(0x80800000);
                            context.DrawGeometry(_brush, _pen, geo);
                        }
                        else
                        {
                            byte ele = b.Elevation;
                            _text.Text = ele.ToString("X2");
                            _brush.Color = Color.FromUInt32(_elevationColors[ele]);
                            context.FillRectangle(_brush, r2);
                            context.DrawRectangle(_pen, r2);
                            context.DrawText(Brushes.Black, new Point(bx, by), _text);
                        }
                    }
                }
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_layout != null)
            {
                return _layout.BlocksBitmap.Size;
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_layout != null)
            {
                return _layout.BlocksBitmap.Size;
            }
            return new Size();
        }

        private void Set(int x, int y)
        {
            Map.Layout.Block b = _layout.Blocks[y][x];
            if (_passageShown)
            {
                if (b.Passage != _passage)
                {
                    b.Passage = _passage;
                    InvalidateVisual();
                }
            }
            else
            {
                if (b.Elevation != _selectedElevation)
                {
                    b.Elevation = _selectedElevation;
                    InvalidateVisual();
                }
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_layout != null)
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
                            Set((int)pos.X / 16, (int)pos.Y / 16);
                            e.Handled = true;
                        }
                        break;
                    }
                    case PointerUpdateKind.MiddleButtonPressed:
                    {
                        Point pos = pp.Position;
                        if (Bounds.TemporaryFix_PointerInControl(pos))
                        {
                            int destX = (int)pos.X / 16;
                            int destY = (int)pos.Y / 16;
                            Map.Layout.Block[][] outArr = _layout.Blocks;
                            if (_passageShown)
                            {
                                LayoutBlockPassage oldPassage = outArr[destY][destX].Passage;
                                LayoutBlockPassage newPassage = _passage;
                                if (oldPassage != newPassage)
                                {
                                    int width = _layout.Width;
                                    int height = _layout.Height;
                                    void Fill(int x, int y)
                                    {
                                        if (x >= 0 && x < width && y >= 0 && y < height)
                                        {
                                            Map.Layout.Block b = outArr[y][x];
                                            if (b.Passage == oldPassage)
                                            {
                                                b.Passage = newPassage;
                                                Fill(x, y + 1);
                                                Fill(x, y - 1);
                                                Fill(x + 1, y);
                                                Fill(x - 1, y);
                                            }
                                        }
                                    }
                                    Fill(destX, destY);
                                    InvalidateVisual();
                                }
                            }
                            else
                            {
                                byte oldElevation = outArr[destY][destX].Elevation;
                                byte newElevation = _selectedElevation;
                                if (oldElevation != newElevation)
                                {
                                    int width = _layout.Width;
                                    int height = _layout.Height;
                                    void Fill(int x, int y)
                                    {
                                        if (x >= 0 && x < width && y >= 0 && y < height)
                                        {
                                            Map.Layout.Block b = outArr[y][x];
                                            if (b.Elevation == oldElevation)
                                            {
                                                b.Elevation = newElevation;
                                                Fill(x, y + 1);
                                                Fill(x, y - 1);
                                                Fill(x + 1, y);
                                                Fill(x - 1, y);
                                            }
                                        }
                                    }
                                    Fill(destX, destY);
                                    InvalidateVisual();
                                }
                            }
                            e.Handled = true;
                        }
                        break;
                    }
                    case PointerUpdateKind.RightButtonPressed:
                    {
                        Point pos = pp.Position;
                        if (Bounds.TemporaryFix_PointerInControl(pos))
                        {
                            Map.Layout.Block b = _layout.Blocks[(int)pos.Y / 16][(int)pos.X / 16];
                            if (_passageShown)
                            {
                                Passage = b.Passage;
                            }
                            else
                            {
                                SelectedElevation = b.Elevation;
                            }
                            e.Handled = true;
                        }
                        break;
                    }
                }
            }
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_layout != null && _isDrawing)
            {
                PointerPoint pp = e.GetCurrentPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        Set((int)pos.X / 16, (int)pos.Y / 16);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_layout != null && _isDrawing)
            {
                PointerPoint pp = e.GetCurrentPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                {
                    _isDrawing = false;
                    e.Handled = true;
                }
            }
        }

        private void RemoveLayoutEvents()
        {
            if (_layout != null)
            {
                _layout.OnDrew -= MapLayout_OnDrew;
            }
        }
        public void Dispose()
        {
            PropertyChanged = null;
            RemoveLayoutEvents();
        }

        private LayoutBlockPassage GetFlag(object parameter)
        {
            switch (parameter)
            {
                case "O": return LayoutBlockPassage.AllowOccupancy;
                case "SW": return LayoutBlockPassage.SouthwestPassage;
                case "SE": return LayoutBlockPassage.SoutheastPassage;
                case "NW": return LayoutBlockPassage.NorthwestPassage;
                case "NE": return LayoutBlockPassage.NortheastPassage;
                default: throw new ArgumentOutOfRangeException(nameof(parameter));
            }
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return _passage.HasFlag(GetFlag(parameter));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            LayoutBlockPassage flag = GetFlag(parameter);
            if (b)
            {
                _passage |= flag;
            }
            else
            {
                _passage &= ~flag;
            }
            return _passage;
        }
    }
}
