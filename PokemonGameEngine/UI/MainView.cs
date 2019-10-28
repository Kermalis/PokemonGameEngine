using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Overworld;
using Kermalis.PokemonGameEngine.Util;
using System;

namespace Kermalis.PokemonGameEngine.UI
{
    public sealed class MainView : Control, IDisposable
    {
        public const int RenderWidth = 480;
        public const int RenderHeight = 256;
        private readonly bool _showFPS = true;

        private bool _isDisposed;
        private readonly WriteableBitmap _screen;
        private readonly Size _screenSize;
        private readonly Stretch _stretch;
        private readonly IDisposable _clock;
        private TimeSpan _lastTime;
        private double _fps;
        private bool _readyForRender = true;

        private readonly Font _font;
        private readonly uint[] _fontColors = new uint[] { 0x00000000, 0xFFFFFFFF, 0xFF848484 };

        public MainView()
        {
            _font = new Font("TestFont.kermfont");
            var map = new Map("TestMapC");
            const int x = 15;
            const int y = 9;
            Obj.Camera.Map = map;
            Obj.Camera.X = x;
            Obj.Camera.Y = y;
            CharacterObj.Player.Map = map;
            CharacterObj.Player.X = x;
            CharacterObj.Player.Y = y;
            map.Characters.Add(CharacterObj.Player);
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
                if (_showFPS)
                {
                    _fps = 60000 / time.Subtract(_lastTime).TotalMilliseconds;
                }
                _lastTime = time;
                using (ILockedFramebuffer l = _screen.Lock())
                {
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    RenderUtil.Fill(bmpAddress, RenderWidth, RenderHeight, 0, 0, RenderWidth, RenderHeight, 0xFF000000);
                    Map.Draw(bmpAddress, RenderWidth, RenderHeight);
                    if (_showFPS)
                    {
                        RenderUtil.Draw(bmpAddress, RenderWidth, RenderHeight, 0, 0, ((int)_fps).ToString(), _font, _fontColors);
                    }
                }
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

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _screen.Dispose();
                _clock.Dispose();
            }
        }
    }
}
