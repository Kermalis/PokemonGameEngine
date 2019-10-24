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
        private readonly Tileset _tileset;
        private readonly Blockset _blockset;
        private readonly uint[][][] _tempPlayerSpriteSheet;

        public MainView()
        {
            _font = new Font("TestFont.kermfont");
            _tileset = new Tileset("Tileset.TestTiles.png");
            _blockset = new Blockset();
            _tempPlayerSpriteSheet = RenderUtil.LoadSpriteSheet("TestNPC.png", 32, 32);
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
                    RenderUtil.Fill(bmpAddress, RenderWidth, RenderHeight, 0, 0, RenderWidth, RenderHeight, 0xFF70C0A0);

                    const int cameraX = 100;
                    const int cameraY = 10;
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[0], cameraX + 0, cameraY + 0);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[1], cameraX + 16, cameraY + 0);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[1], cameraX + 32, cameraY + 0);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[2], cameraX + 48, cameraY + 0);

                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[3], cameraX + 0, cameraY + 16);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[4], cameraX + 16, cameraY + 16);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[4], cameraX + 32, cameraY + 16);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[5], cameraX + 48, cameraY + 16);

                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[6], cameraX + 0, cameraY + 32);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[7], cameraX + 16, cameraY + 32);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[8], cameraX + 32, cameraY + 32);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[9], cameraX + 48, cameraY + 32);

                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[10], cameraX + 0, cameraY + 48);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[11], cameraX + 16, cameraY + 48);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[12], cameraX + 32, cameraY + 48);
                    _tileset.DrawBlock(bmpAddress, RenderWidth, RenderHeight, _blockset[13], cameraX + 48, cameraY + 48);

                    RenderUtil.Draw(bmpAddress, RenderWidth, RenderHeight, cameraX + 16 - 8, cameraY + 64 - 16, _tempPlayerSpriteSheet[0], false, false); // Temporarily rendered above all

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
