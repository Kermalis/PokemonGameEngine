using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Sound;
using SDL2;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Core
{
    internal static class Engine
    {
        public static bool QuitRequested;

        private static IntPtr _controller;
        private static int _controllerId;

        #region Temp Tasks are here for now since some resources are destroyed in the finalizer, which is the wrong thread. Need to manually control resources

        private static readonly List<Action> _tempTasks = new();
        public static void AddTempTask(Action a)
        {
            lock (_tempTasks)
            {
                _tempTasks.Add(a);
            }
        }
        private static void DoTempTasks()
        {
            lock (_tempTasks)
            {
                foreach (Action a in _tempTasks)
                {
                    a();
                }
                _tempTasks.Clear();
            }
        }

        #endregion

        // Initializes the first callback, the window, and instances
        private static void Init()
        {
            Display.Init(); // Inits SDL
            SoundMixer.Init(); // Init SDL Audio
            AttachFirstController();
            AssetLoader.InitBattleEngine();
            RenderManager.Init();
            _ = new Game();
        }
        // Entry point of the game and main loop
        private static void Main()
        {
            Init();

            // Main loop
            DateTime time = DateTime.Now;
            while (!QuitRequested) // Break if quit was requested by game
            {
                DoTempTasks();
                InputManager.Update();

                // Grab all OS events
                if (HandleOSEvents())
                {
                    break; // Break if quit was requested by OS
                }

                if (Display.PrepareFrame(ref time))
                {
                    continue; // Skip current frame if it returned true
                }
                Game.Instance.RunCallback();
                Display.PresentFrame();
            }

            // Quitting
            Quit();
        }
        // Handles freeing resources once the game is closing
        private static void Quit()
        {
            SoundMixer.Quit();
            AssimpLoader.Quit();
            SDL.SDL_GameControllerClose(_controller);

            Display.Quit(); // Quits SDL altogether
        }

        private static bool HandleOSEvents()
        {
            while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                    {
                        QuitRequested = true;
                        return true;
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
                            InputManager.OnAxis(e.caxis);
                        }
                        break;
                    }
                    case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    {
                        if (e.cbutton.which == _controllerId)
                        {
                            var button = (SDL.SDL_GameControllerButton)e.cbutton.button;
                            InputManager.OnButtonDown(button, true);
                        }
                        break;
                    }
                    case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                    {
                        if (e.cbutton.which == _controllerId)
                        {
                            var button = (SDL.SDL_GameControllerButton)e.cbutton.button;
                            InputManager.OnButtonDown(button, false);
                        }
                        break;
                    }
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                    {
                        SDL.SDL_Keycode sym = e.key.keysym.sym;
                        switch (sym)
                        {
                            case SDL.SDL_Keycode.SDLK_F12: Display.SaveScreenshot(); break;
                            default: InputManager.OnKeyDown(sym, true); break;
                        }
                        break;
                    }
                    case SDL.SDL_EventType.SDL_KEYUP:
                    {
                        SDL.SDL_Keycode sym = e.key.keysym.sym;
                        InputManager.OnKeyDown(sym, false);
                        break;
                    }
                }
            }
            return false;
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
    }
}
