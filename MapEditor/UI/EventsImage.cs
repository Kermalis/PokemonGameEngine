using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Kermalis.MapEditor.Core;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.MapEditor.UI
{
    public sealed class EventsImage : Control, IDisposable
    {
        private Map _map;
        internal Map Map
        {
            get => _map;
            set
            {
                if (_map != value)
                {
                    RemoveLayoutEvents();
                    _map = value;
                    value.MapLayout.OnDrew += MapLayout_OnDrew;
                    InvalidateMeasure();
                    InvalidateVisual();
                }
            }
        }

        private readonly SolidColorBrush _brush;
        private readonly Pen _pen;
        private readonly FormattedText _text;

        public EventsImage()
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
            if (_map != null)
            {
                Map.Layout layout = _map.MapLayout;
                IBitmap source = layout.BlocksBitmap;
                var viewPort = new Rect(Bounds.Size);
                var r = new Rect(source.Size);
                Rect destRect = viewPort.CenterRect(r).Intersect(viewPort);
                Rect sourceRect = r.CenterRect(new Rect(destRect.Size));

                context.DrawImage(source, sourceRect, destRect);

                Map.Events events = _map.MapEvents;
                _brush.Color = Color.FromUInt32(0x80800080);
                _text.Text = "W";
                foreach (Map.Events.WarpEvent e in events.Warps)
                {
                    int ex = e.X * Overworld.Block_NumPixelsX;
                    int ey = e.Y * Overworld.Block_NumPixelsY;
                    var r2 = new Rect(ex, ey, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY);
                    context.FillRectangle(_brush, r2);
                    context.DrawRectangle(_pen, r2);
                    context.DrawText(Brushes.White, new Point(ex, ey), _text);
                }
                _brush.Color = Color.FromUInt32(0x80808000);
                _text.Text = "O";
                foreach (Map.Events.ObjEvent e in events.Objs)
                {
                    int ex = e.X * Overworld.Block_NumPixelsX;
                    int ey = e.Y * Overworld.Block_NumPixelsY;
                    var r2 = new Rect(ex, ey, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY);
                    context.FillRectangle(_brush, r2);
                    context.DrawRectangle(_pen, r2);
                    context.DrawText(Brushes.White, new Point(ex, ey), _text);
                }
                _brush.Color = Color.FromUInt32(0x80008000);
                _text.Text = "S";
                foreach (Map.Events.ScriptEvent e in events.ScriptTiles)
                {
                    int ex = e.X * Overworld.Block_NumPixelsX;
                    int ey = e.Y * Overworld.Block_NumPixelsY;
                    var r2 = new Rect(ex, ey, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY);
                    context.FillRectangle(_brush, r2);
                    context.DrawRectangle(_pen, r2);
                    context.DrawText(Brushes.White, new Point(ex, ey), _text);
                }
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_map != null)
            {
                return _map.MapLayout.BlocksBitmap.Size;
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_map != null)
            {
                return _map.MapLayout.BlocksBitmap.Size;
            }
            return new Size();
        }

        private void RemoveLayoutEvents()
        {
            if (_map != null)
            {
                _map.MapLayout.OnDrew -= MapLayout_OnDrew;
            }
        }
        public void Dispose()
        {
            RemoveLayoutEvents();
        }
    }
}
