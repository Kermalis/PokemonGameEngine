using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;

namespace Kermalis.MapEditor.UI
{
    public sealed class TileLayerImage : Control
    {
        public event EventHandler<Blockset.Block.Tile> SelectionCompleted;

        private readonly Blockset.Block.Tile[][] _tiles;
        private readonly WriteableBitmap _bitmap;
        private readonly Size _bitmapSize;
        private readonly double _scale;

        public TileLayerImage(double scale)
        {
            _scale = scale;
            _tiles = new Blockset.Block.Tile[2][];
            for (int i = 0; i < 2; i++)
            {
                _tiles[i] = new Blockset.Block.Tile[2];
            }
            _bitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888);
            _bitmapSize = new Size(16, 16);

            PointerPressed += OnPointerPressed;
        }

        public override void Render(DrawingContext context)
        {
            var viewPort = new Rect(Bounds.Size);
            Rect destRect = viewPort.CenterRect(new Rect(_bitmapSize * _scale)).Intersect(viewPort);
            Rect sourceRect = new Rect(_bitmapSize).CenterRect(new Rect(destRect.Size / _scale));

            context.DrawImage(_bitmap, 1, sourceRect, destRect);
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            return _bitmapSize * _scale;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            return _bitmapSize * _scale;
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            PointerPoint pp = e.GetPointerPoint(this);
            if (pp.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            {
                Point pos = pp.Position;
                if (Bounds.TemporaryFix_RectContains(pos))
                {
                    SelectionCompleted?.Invoke(this, _tiles[(int)(pos.Y / _scale) / 8][(int)(pos.X / _scale) / 8]);
                    e.Handled = true;
                }
            }
        }
    }
}
