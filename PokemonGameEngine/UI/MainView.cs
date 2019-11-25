using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Overworld;
using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;

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

        private readonly Font _font;
        private readonly uint[] _fontColors = new uint[] { 0x00000000, 0xFFFFFFFF, 0xFF848484 };

        public MainView()
        {
            _font = new Font("TestFont.kermfont");
            var map = Map.LoadOrGet(0);
            const int x = 15;
            const int y = 9;
            Obj.Camera.X = x;
            Obj.Camera.Y = y;
            Obj.Camera.Map = map;
            map.Objs.Add(Obj.Camera);
            Obj.Player.X = x;
            Obj.Player.Y = y;
            Obj.Player.Map = map;
            map.Objs.Add(Obj.Player);
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
                    RenderUtil.Fill(bmpAddress, RenderWidth, RenderHeight, 0, 0, RenderWidth, RenderHeight, 0xFF000000);
                    List<Obj> list = Obj.LoadedObjs;
                    int count = list.Count;
                    for (int i = 0; i < count; i++)
                    {
                        list[i].UpdateMovement();
                    }
                    if (Obj.Camera.MovementTimer == 0 && Obj.Player.MovementTimer == 0)
                    {
                        bool down = InputManager.IsPressed(Key.Down);
                        bool up = InputManager.IsPressed(Key.Up);
                        bool left = InputManager.IsPressed(Key.Left);
                        bool right = InputManager.IsPressed(Key.Right);
                        if (down || up || left || right)
                        {
                            Obj.FacingDirection facing;
                            if (down)
                            {
                                if (left)
                                {
                                    facing = Obj.FacingDirection.Southwest;
                                }
                                else if (right)
                                {
                                    facing = Obj.FacingDirection.Southeast;
                                }
                                else
                                {
                                    facing = Obj.FacingDirection.South;
                                }
                            }
                            else if (up)
                            {
                                if (left)
                                {
                                    facing = Obj.FacingDirection.Northwest;
                                }
                                else if (right)
                                {
                                    facing = Obj.FacingDirection.Northeast;
                                }
                                else
                                {
                                    facing = Obj.FacingDirection.North;
                                }
                            }
                            else if (left)
                            {
                                facing = Obj.FacingDirection.West;
                            }
                            else
                            {
                                facing = Obj.FacingDirection.East;
                            }
                            bool run = InputManager.IsPressed(Key.B);
                            Obj.Camera.Move(facing, run);
                            Obj.Player.Move(facing, run);
                        }
                    }
                    Map.Draw(bmpAddress, RenderWidth, RenderHeight);
                    if (_showFPS)
                    {
                        RenderUtil.Draw(bmpAddress, RenderWidth, RenderHeight, 0, 0, ((int)Math.Round(1000 / time.Subtract(_lastRenderTime).TotalMilliseconds)).ToString(), _font, _fontColors);
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
