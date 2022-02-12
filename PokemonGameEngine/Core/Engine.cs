using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Sound;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using System;
using System.Runtime.CompilerServices;

namespace Kermalis.PokemonGameEngine.Core
{
    internal static class Engine
    {
        public static bool QuitRequested { get; private set; }
        public static event Action OnQuitRequested;

        // Initializes the first callback, the window, and instances
        private static void Init()
        {
            Configuration.Default.PreferContiguousImageBuffers = true;
            RuntimeHelpers.RunClassConstructor(typeof(Display).TypeHandle); // Inits Display static constructor & SDL
            RuntimeHelpers.RunClassConstructor(typeof(SoundMixer).TypeHandle); // Init SoundMixer static constructor & SDL Audio
            RuntimeHelpers.RunClassConstructor(typeof(AssimpLoader).TypeHandle); // Init AssimpLoader static constructor
            AssetLoader.InitBattleEngineProvider();
            InputManager.Init(); // Attach controller if there is one
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
                InputManager.Prepare();

                // Grab all OS events
                if (HandleOSEvents())
                {
                    break; // Break if quit was requested by OS
                }

                if (!Display.PrepareFrame(ref time))
                {
                    Game.Instance.RunCallback();
                    Display.PresentFrame();
                }
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

        private static unsafe bool HandleOSEvents()
        {
            Event e;
            while (Display.SDL.PollEvent(&e) != 0)
            {
                switch ((EventType)e.Type)
                {
                    case EventType.Quit:
                    {
                        RequestQuit();
                        return true;
                    }
                    case EventType.Controllerdeviceadded:
                    {
                        Controller.OnControllerAdded();
                        break;
                    }
                    case EventType.Controllerdeviceremoved:
                    {
                        Controller.OnControllerRemoved(e.Cdevice.Which);
                        break;
                    }
                    case EventType.Controlleraxismotion:
                    {
                        Controller.OnAxisChanged(e.Caxis);
                        break;
                    }
                    case EventType.Controllerbuttondown:
                    {
                        Controller.OnButtonChanged(e.Cbutton, true);
                        break;
                    }
                    case EventType.Controllerbuttonup:
                    {
                        Controller.OnButtonChanged(e.Cbutton, false);
                        break;
                    }
                    case EventType.Keydown:
                    {
                        // Don't accept repeat events
                        if (e.Key.Repeat == 0)
                        {
                            Keyboard.OnKeyChanged((KeyCode)e.Key.Keysym.Sym, true);
                        }
                        break;
                    }
                    case EventType.Keyup:
                    {
                        Keyboard.OnKeyChanged((KeyCode)e.Key.Keysym.Sym, false);
                        break;
                    }
                    case EventType.Mousebuttondown:
                    {
                        Mouse.OnButtonDown(e.Button.Button, true);
                        break;
                    }
                    case EventType.Mousebuttonup:
                    {
                        Mouse.OnButtonDown(e.Button.Button, false);
                        break;
                    }
                    case EventType.Mousemotion:
                    {
                        Mouse.OnMove(e.Motion);
                        break;
                    }
                    case EventType.Windowevent:
                    {
                        switch ((WindowEventID)e.Window.Event)
                        {
                            case WindowEventID.WindoweventResized:
                            {
                                Display.AutosizeWindow = false;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            return false;
        }

        public static void RequestQuit()
        {
            QuitRequested = true;
            OnQuitRequested?.Invoke();
        }
    }
}
