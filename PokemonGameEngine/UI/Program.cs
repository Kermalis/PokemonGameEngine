using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Util;
using SDL2;
using System;
using System.Threading;

namespace Kermalis.PokemonGameEngine.UI
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            new Program().MainLoop();
        }

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
        public const int NumTicksPerSecond = 20;
        public const int MaxFPS = 60;
        public static readonly bool _showFPS = true;

        private readonly object _threadLockObj = new object();
        private readonly IntPtr _window;
        private readonly IntPtr _renderer;
        private readonly IntPtr _screen;
        private bool _quit;

        private Program()
        {
            Utils.SetWorkingDirectory(string.Empty);

            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            _window = SDL.SDL_CreateWindow("Pokémon Game Engine", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, RenderWidth, RenderHeight, 0); // Resizable or fullscreen break when resized/minimized/maximized
            _renderer = SDL.SDL_CreateRenderer(_window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            _screen = SDL.SDL_CreateTexture(_renderer, SDL.SDL_PIXELFORMAT_ABGR8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, RenderWidth, RenderHeight);

            new Game(); // Init game
            new Thread(LogicTick) { Name = "Logic Thread" }.Start();
            new Thread(RenderTick) { Name = "Render Thread" }.Start();
        }

        private void MainLoop()
        {
            while (!_quit)
            {
                while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
                {
                    switch (e.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                        {
                            _quit = true;
                            break;
                        }
                        case SDL.SDL_EventType.SDL_KEYDOWN:
                        {
                            InputManager.OnKeyDown(e, true);
                            break;
                        }
                        case SDL.SDL_EventType.SDL_KEYUP:
                        {
                            InputManager.OnKeyDown(e, false);
                            break;
                        }
                    }
                }
            }

            SDL.SDL_DestroyTexture(_screen);
            SDL.SDL_DestroyRenderer(_renderer);
            SDL.SDL_DestroyWindow(_window);
            SDL.SDL_Quit();
        }

        private void LogicTick()
        {
            while (!_quit)
            {
                lock (_threadLockObj)
                {
                    Game.Instance.LogicTick();
                }
                Thread.Sleep(1_000 / NumTicksPerSecond);
            }
        }

        private unsafe void RenderTick()
        {
            var time = new TimeBarrier(MaxFPS);
            time.Start();

            DateTime lastRenderTime = DateTime.Now;
            while (!_quit)
            {
                DateTime now = DateTime.Now;
                SDL.SDL_LockTexture(_screen, IntPtr.Zero, out IntPtr pixels, out _);
                lock (_threadLockObj)
                {
                    Game.Instance.RenderTick((uint*)pixels.ToPointer(), RenderWidth, RenderHeight, _showFPS ? ((int)Math.Round(1_000 / now.Subtract(lastRenderTime).TotalMilliseconds)).ToString() : null);
                }
                SDL.SDL_UnlockTexture(_screen);
                SDL.SDL_RenderClear(_renderer);
                SDL.SDL_RenderCopy(_renderer, _screen, IntPtr.Zero, IntPtr.Zero);
                SDL.SDL_RenderPresent(_renderer);
                lastRenderTime = now;
                time.Wait();
            }
            time.Stop();
        }
    }
}
