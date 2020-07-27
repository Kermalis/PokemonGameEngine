using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace Kermalis.PokemonGameEngine.UI
{
    public sealed class MainView : Control, IDisposable
    {
        public const int RenderWidth = 384;
        public const int RenderHeight = 216;
        private readonly bool _showFPS = true;

        private bool _isDisposed;
        private readonly WriteableBitmap _screen;
        private readonly Size _screenSize;
        private readonly Stretch _stretch;
        private readonly IDisposable _clock;
        private TimeSpan _lastRenderTime;
        private bool _readyForRender = true;

        public MainView()
        {
            _screen = new WriteableBitmap(new PixelSize(RenderWidth, RenderHeight), new Vector(96, 96), PixelFormat.Bgra8888);
            _screenSize = new Size(RenderWidth, RenderHeight);
            _stretch = Stretch.Uniform;
            _clock = new Clock().Subscribe(RenderTick);
        }

        private unsafe void RenderTick(TimeSpan time)
        {
            if (!_isDisposed && _readyForRender)
            {
                _readyForRender = false;
                using (ILockedFramebuffer l = _screen.Lock())
                {
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    Game.Game.Instance.RenderTick(bmpAddress, RenderWidth, RenderHeight);
                    if (_showFPS)
                    {
                        Game.Game.Instance.RenderFPS(bmpAddress, RenderWidth, RenderHeight, (int)Math.Round(1000 / time.Subtract(_lastRenderTime).TotalMilliseconds));
                    }
                }
                _lastRenderTime = time;
                InvalidateVisual();
                _readyForRender = true;
            }
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
                context.DrawImage(_screen, 1, sourceRect, destRect);
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
                _clock.Dispose();
                _screen.Dispose();
            }
        }
    }
}
