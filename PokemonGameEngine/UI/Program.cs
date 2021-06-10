using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.Util;
using SDL2;
using System;
using System.Threading;

namespace Kermalis.PokemonGameEngine.UI
{
    internal static class Program
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
        public const int NumTicksPerSecond = 20;
        public const int MaxFPS = 60;
#if DEBUG
        public static readonly bool _showFPS = true;
#endif
        public static DateTime LogicTickTime { get; private set; }
        public static DateTime RenderTickTime { get; private set; }
        public static TimeSpan RenderTimeSinceLastFrame { get; private set; }

        private static readonly object _threadLockObj = new();
        private static readonly IntPtr _window;
        private static IntPtr _renderer;
        private static IntPtr _screen;
        private static bool _quit;
        private static bool _tickQuit1, _tickQuit2;
        private static IntPtr _controller;
        private static int _controllerId;

        static Program()
        {
            // Battle engine
            Utils.SetWorkingDirectory(string.Empty);

            // SDL 2
            SDL.SDL_Init(SDL.SDL_INIT_AUDIO | SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER);
            SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG);

            _window = SDL.SDL_CreateWindow("Pokémon Game Engine", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, RenderWidth, RenderHeight, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            CreateRendererAndScreen();

            AttachFirstController();

            // Init SDL Audio
            SoundMixer.Init();

            // Game
            LogicTickTime = DateTime.Now; // Setting for DayTint static constructor
            _ = new Game(); // Init game
            new Thread(LogicTick) { Name = "Logic Thread" }.Start();
            new Thread(RenderTick) { Name = "Render Thread", Priority = ThreadPriority.Highest }.Start();
        }

        private static void CreateRendererAndScreen()
        {
            IntPtr r = SDL.SDL_CreateRenderer(_window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            _renderer = r;
            _screen = SDL.SDL_CreateTexture(r, SDL.SDL_PIXELFORMAT_ABGR8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, RenderWidth, RenderHeight);
        }
        private static void AttachFirstController()
        {
            int num = SDL.SDL_NumJoysticks();
            for (int i = 0; i < num; i++)
            {
                if (SDL.SDL_IsGameController(i) == SDL.SDL_bool.SDL_TRUE)
                {
                    _controller = SDL.SDL_GameControllerOpen(i);
                    if (_controller != IntPtr.Zero)
                    {
                        _controllerId = SDL.SDL_JoystickInstanceID(SDL.SDL_GameControllerGetJoystick(_controller));
                        break;
                    }
                }
            }
        }

        [STAThread]
        private static void Main()
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
                        case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        {
                            if (e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                            {
                                lock (_threadLockObj)
                                {
                                    SDL.SDL_DestroyTexture(_screen);
                                    SDL.SDL_DestroyRenderer(_renderer);
                                    CreateRendererAndScreen();
                                }
                            }
                            break;
                        }
                        case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                        {
                            if (e.cdevice.which == _controllerId)
                            {
                                SDL.SDL_GameControllerClose(_controller);
                                _controller = IntPtr.Zero;
                                _controllerId = -1;
                            }
                            break;
                        }
                        case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                        {
                            if (_controller == IntPtr.Zero)
                            {
                                AttachFirstController();
                            }
                            break;
                        }
                        case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                        {
                            if (e.caxis.which == _controllerId)
                            {
                                InputManager.OnAxis(e);
                            }
                            break;
                        }
                        case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                        {
                            if (e.cbutton.which == _controllerId)
                            {
                                InputManager.OnButtonDown(e, true);
                            }
                            break;
                        }
                        case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                        {
                            if (e.cbutton.which == _controllerId)
                            {
                                InputManager.OnButtonDown(e, false);
                            }
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
            while (!_tickQuit1 && !_tickQuit2)
            {
                ; // Wait for ticks to quit
            }

            SoundMixer.DeInit();

            SDL.SDL_DestroyTexture(_screen);
            SDL.SDL_DestroyRenderer(_renderer);
            SDL.SDL_DestroyWindow(_window);
            SDL.SDL_GameControllerClose(_controller);
            SDL_image.IMG_Quit();
            SDL.SDL_Quit();
        }

        private static void LogicTick()
        {
            DateTime lastTickTime = DateTime.Now;
            while (!_quit)
            {
                lock (_threadLockObj)
                {
                    if (_quit)
                    {
                        goto bottom;
                    }
                    LogicTickTime = DateTime.Now;
                    if (LogicTickTime <= lastTickTime)
                    {
                        Console.WriteLine("Time went back!");
                    }
                    InputManager.LogicTick();
                    Game.Instance.LogicTick();
                }
                lastTickTime = LogicTickTime;
                Thread.Sleep(1_000 / NumTicksPerSecond);
            }
        bottom:
            _tickQuit1 = true;
        }

        private static unsafe void RenderTick()
        {
            var time = new TimeBarrier(MaxFPS);
            time.Start();

            DateTime lastRenderTime = DateTime.Now;
            while (!_quit)
            {
                lock (_threadLockObj)
                {
                    if (_quit)
                    {
                        goto bottom;
                    }
                    RenderTickTime = DateTime.Now;
                    if (RenderTickTime <= lastRenderTime)
                    {
                        RenderTimeSinceLastFrame = new TimeSpan(0, 0, 0, 0, 1_000 / MaxFPS);
                    }
                    else
                    {
                        RenderTimeSinceLastFrame = RenderTickTime.Subtract(lastRenderTime);
                    }
                    IntPtr s = _screen;
                    IntPtr r = _renderer;
                    SDL.SDL_LockTexture(s, IntPtr.Zero, out IntPtr pixels, out _);
                    Game.Instance.RenderTick((uint*)pixels.ToPointer(), RenderWidth, RenderHeight
#if DEBUG
                        //, InputManager.Debug_GetKeys(), GUI.Font.DefaultSmall, GUI.Font.DefaultWhite_DarkerOutline_I
                        , _showFPS ? ((int)Math.Round(1_000 / RenderTimeSinceLastFrame.TotalMilliseconds)).ToString() : null, GUI.Font.Default, GUI.Font.DefaultRed_O
#endif
                        );
                    SDL.SDL_UnlockTexture(s);
                    SDL.SDL_RenderClear(r);
                    SDL.SDL_RenderCopy(r, s, IntPtr.Zero, IntPtr.Zero);
                    SDL.SDL_RenderPresent(r);
                }
                lastRenderTime = RenderTickTime;
                time.Wait();
            }
        bottom:
            time.Stop();
            _tickQuit2 = true;
        }
    }
}
