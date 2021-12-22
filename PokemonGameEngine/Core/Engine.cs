using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Sound;
using SDL2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Core
{
    internal static class Engine
    {
        public static bool QuitRequested;

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
            RuntimeHelpers.RunClassConstructor(typeof(Display).TypeHandle); // Inits Display static constructor & SDL
            RuntimeHelpers.RunClassConstructor(typeof(SoundMixer).TypeHandle); // Init SoundMixer static constructor & SDL Audio
            RuntimeHelpers.RunClassConstructor(typeof(AssimpLoader).TypeHandle); // Init AssimpLoader static constructor
            InputManager.Init(); // Attach controller if there is one
            AssetLoader.InitBattleEngineProvider();
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
                InputManager.Prepare();

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
            InputManager.Quit();
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
                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    {
                        Controller.OnControllerAdded();
                        break;
                    }
                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                    {
                        Controller.OnControllerRemoved(e.cdevice.which);
                        break;
                    }
                    case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                    {
                        Controller.OnAxisChanged(e.caxis);
                        break;
                    }
                    case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    {
                        Controller.OnButtonChanged(e.cbutton, true);
                        break;
                    }
                    case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                    {
                        Controller.OnButtonChanged(e.cbutton, false);
                        break;
                    }
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                    {
                        // Don't accept repeat events
                        if (e.key.repeat == 0)
                        {
                            Keyboard.OnKeyChanged(e.key.keysym.sym, true);
                        }
                        break;
                    }
                    case SDL.SDL_EventType.SDL_KEYUP:
                    {
                        Keyboard.OnKeyChanged(e.key.keysym.sym, false);
                        break;
                    }
                }
            }
            return false;
        }
    }
}
