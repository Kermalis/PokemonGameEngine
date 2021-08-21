﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.World;
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

        private readonly ElevationConverter _elevationConverter = new();
        private readonly PassageConverter _passageConverter = new();

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
        public byte _elevations;
        public byte Elevations
        {
            get => _elevations;
            set
            {
                if (value != _elevations)
                {
                    _elevations = value;
                    _elevationConverter.Elevations = value;
                    OnPropertyChanged(nameof(Elevations));
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
                    _passageConverter.Flags = value;
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

        private static readonly Dictionary<byte, uint> _elevationsColors;
        private static readonly Dictionary<LayoutBlockPassage, Geometry> _passageGeometries;
        static MovementImage()
        {
            var r = new Random(1114);
            _elevationsColors = new Dictionary<byte, uint>(byte.MaxValue + 1);
            byte b = 0;
            while (true)
            {
                _elevationsColors.Add(b, (uint)(0x80000000 + r.Next(0, 0x1000000)));
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
                // Spaghetti town, don't worry about it :)
                string s;
                if (i.HasFlag(LayoutBlockPassage.AllowOccupancy))
                {
                    s = string.Empty;
                    if (!i.HasFlag(LayoutBlockPassage.SouthwestPassage))
                    {
                        s += $"M 0,{Overworld.Block_NumPixelsY / 2 + 1} V {Overworld.Block_NumPixelsY} H {Overworld.Block_NumPixelsX / 2} L 0,{Overworld.Block_NumPixelsY / 2 + 1}";
                    }
                    if (!i.HasFlag(LayoutBlockPassage.SoutheastPassage))
                    {
                        s += $"M {Overworld.Block_NumPixelsX / 2 + 1},{Overworld.Block_NumPixelsY} H {Overworld.Block_NumPixelsX} V {Overworld.Block_NumPixelsY / 2 + 1} L {Overworld.Block_NumPixelsX / 2 + 1},{Overworld.Block_NumPixelsY}";
                    }
                    if (!i.HasFlag(LayoutBlockPassage.NorthwestPassage))
                    {
                        s += $"M {Overworld.Block_NumPixelsX / 2},0 H 0 V {Overworld.Block_NumPixelsY / 2} L {Overworld.Block_NumPixelsX / 2},0";
                    }
                    if (!i.HasFlag(LayoutBlockPassage.NortheastPassage))
                    {
                        s += $"M {Overworld.Block_NumPixelsX},{Overworld.Block_NumPixelsY / 2} V 0 H {Overworld.Block_NumPixelsX / 2 + 1} L {Overworld.Block_NumPixelsX},{Overworld.Block_NumPixelsY / 2}";
                    }
                }
                else
                {
                    s = (i.HasFlag(LayoutBlockPassage.SouthwestPassage) ? $"M 0,{Overworld.Block_NumPixelsY / 2 + 1} L {Overworld.Block_NumPixelsX / 2},{Overworld.Block_NumPixelsY}" : $"M 0,{Overworld.Block_NumPixelsY} H {Overworld.Block_NumPixelsX / 2}")
                    + (i.HasFlag(LayoutBlockPassage.SoutheastPassage) ? $" H {Overworld.Block_NumPixelsX / 2 + 1} L {Overworld.Block_NumPixelsX},{Overworld.Block_NumPixelsY / 2 + 1}" : $" H {Overworld.Block_NumPixelsX}")
                    + (i.HasFlag(LayoutBlockPassage.NortheastPassage) ? $" V {Overworld.Block_NumPixelsY / 2} L {Overworld.Block_NumPixelsX / 2 + 1},0" : $" V 0")
                    + (i.HasFlag(LayoutBlockPassage.NorthwestPassage) ? $" H {Overworld.Block_NumPixelsX / 2} L 0,{Overworld.Block_NumPixelsY / 2}" : $" H 0")
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
            _text = new FormattedText { Constraint = new Size(Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY), TextAlignment = TextAlignment.Center, Typeface = Typeface.Default };
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
            if (_layout is not null)
            {
                IBitmap source = _layout.BlocksBitmap;
                var viewPort = new Rect(Bounds.Size);
                var r = new Rect(source.Size);
                Rect destRect = viewPort.CenterRect(r).Intersect(viewPort);
                Rect sourceRect = r.CenterRect(new Rect(destRect.Size));

                context.DrawImage(source, sourceRect, destRect);

                for (int y = 0; y < _layout.Height; y++)
                {
                    int by = y * Overworld.Block_NumPixelsY;
                    Map.Layout.Block[] arrY = _layout.Blocks[y];
                    for (int x = 0; x < _layout.Width; x++)
                    {
                        int bx = x * Overworld.Block_NumPixelsX;
                        var r2 = new Rect(bx, by, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY);
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
                            byte ele = b.Elevations;
                            _text.Text = ele.ToString("X2"); // TODO: Better way to show the elevations?
                            _brush.Color = Color.FromUInt32(_elevationsColors[ele]);
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
            if (_layout is not null)
            {
                return _layout.BlocksBitmap.Size;
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_layout is not null)
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
                if (b.Elevations != _elevations)
                {
                    b.Elevations = _elevations;
                    InvalidateVisual();
                }
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_layout is not null)
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
                            Set((int)pos.X / Overworld.Block_NumPixelsX, (int)pos.Y / Overworld.Block_NumPixelsY);
                            e.Handled = true;
                        }
                        break;
                    }
                    case PointerUpdateKind.MiddleButtonPressed:
                    {
                        Point pos = pp.Position;
                        if (Bounds.TemporaryFix_PointerInControl(pos))
                        {
                            int destX = (int)pos.X / Overworld.Block_NumPixelsX;
                            int destY = (int)pos.Y / Overworld.Block_NumPixelsY;
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
                                byte oldElevations = outArr[destY][destX].Elevations;
                                byte newElevations = _elevations;
                                if (oldElevations != newElevations)
                                {
                                    int width = _layout.Width;
                                    int height = _layout.Height;
                                    void Fill(int x, int y)
                                    {
                                        if (x >= 0 && x < width && y >= 0 && y < height)
                                        {
                                            Map.Layout.Block b = outArr[y][x];
                                            if (b.Elevations == oldElevations)
                                            {
                                                b.Elevations = newElevations;
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
                            Map.Layout.Block b = _layout.Blocks[(int)pos.Y / Overworld.Block_NumPixelsY][(int)pos.X / Overworld.Block_NumPixelsX];
                            if (_passageShown)
                            {
                                Passage = b.Passage;
                            }
                            else
                            {
                                Elevations = b.Elevations;
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
            if (_layout is not null && _isDrawing)
            {
                PointerPoint pp = e.GetCurrentPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_PointerInControl(pos))
                    {
                        Set((int)pos.X / Overworld.Block_NumPixelsX, (int)pos.Y / Overworld.Block_NumPixelsY);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_layout is not null && _isDrawing)
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
            if (_layout is not null)
            {
                _layout.OnDrew -= MapLayout_OnDrew;
            }
        }
        public void Dispose()
        {
            PropertyChanged = null;
            RemoveLayoutEvents();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IValueConverter c;
            if (value is LayoutBlockPassage)
            {
                c = _passageConverter;
            }
            else
            {
                c = _elevationConverter;
            }
            return c.Convert(value, targetType, parameter, culture);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IValueConverter c;
            if (targetType == typeof(LayoutBlockPassage))
            {
                c = _passageConverter;
            }
            else
            {
                c = _elevationConverter;
            }
            return c.ConvertBack(value, targetType, parameter, culture);
        }
    }
}
