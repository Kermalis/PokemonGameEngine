using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.PokemonGameEngine.Core;
using System;
using System.Threading;

namespace Kermalis.PokemonGameEngine.UI
{
    public sealed class MainView : Control, IDisposable
    {
        // A block is 16x16 pixels (2x2 tiles, and a tile is 8x8 pixels)
        // You can have different sized blocks and tiles if you wish, but this table is demonstrating defaults
        // GB/GBC         -  160 x 144 resolution (10:9) - 10 x  9   blocks
        // GBA            -  240 x 160 resolution ( 3:2) - 15 x 10   blocks
        // NDS            -  256 x 192 resolution ( 4:3) - 16 x 12   blocks
        // 3DS (Lower)    -  320 x 240 resolution ( 4:3) - 20 x 15   blocks
        // 3DS (Upper)    -  400 x 240 resolution ( 5:3) - 25 x 15   blocks
        // Default below  -  384 x 216 resolution (16:9) - 24 x 13.5 blocks
        public const int RenderWidth = 384;
        public const int RenderHeight = 216;
        private const int MaxFPS = 60; // Avalonia's InvalidateMeasure() is capped at 60fps, even if you call it more often
        private readonly bool _showFPS = true;

        private bool _isDisposed;
        private readonly WriteableBitmap _screen;
        private readonly Size _screenSize;
        private readonly Stretch _stretch;
        private readonly Thread _renderThread;

        public MainView()
        {
            _screen = new WriteableBitmap(new PixelSize(RenderWidth, RenderHeight), new Vector(96, 96), PixelFormat.Bgra8888);
            _screenSize = new Size(RenderWidth, RenderHeight);
            _stretch = Stretch.Uniform;
            _renderThread = new Thread(RenderTick) { Name = "Render Thread" };
            _renderThread.Start();
        }

        private unsafe void RenderTick()
        {
            var time = new TimeBarrier(MaxFPS);
            time.Start();

            DateTime lastRenderTime = DateTime.Now;
            while (!_isDisposed)
            {
                DateTime now = DateTime.Now;
                using (ILockedFramebuffer l = _screen.Lock())
                {
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    Game.Instance.RenderTick(bmpAddress, RenderWidth, RenderHeight);
                    if (_showFPS)
                    {
                        Game.Instance.RenderFPS(bmpAddress, RenderWidth, RenderHeight, (int)Math.Round(1_000 / now.Subtract(lastRenderTime).TotalMilliseconds));
                    }
                }
                InvalidateVisual();
                lastRenderTime = now;
                time.Wait();
            }
            time.Stop();
        }
        public override void Render(DrawingContext context)
        {
            if (!_isDisposed)
            {
                // TODO: Better to calculate this stuff when resizing only
                Size bSize = Bounds.Size;
                Vector scale = _stretch.CalculateScaling(bSize, _screenSize);
                Size scaledSize = _screenSize * scale;
                var viewPort = new Rect(bSize);
                Rect destRect = viewPort.CenterRect(new Rect(scaledSize)).Intersect(viewPort);
                Rect sourceRect = new Rect(_screenSize).CenterRect(new Rect(destRect.Size / scale));
                context.DrawImage(_screen, sourceRect, destRect);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
            {
                return _screenSize;
            }
            else
            {
                return _stretch.CalculateSize(availableSize, _screenSize);
            }
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            return _stretch.CalculateSize(finalSize, _screenSize);
        }

        // TODO: This isn't called
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _screen.Dispose();
            }
        }
    }
}
