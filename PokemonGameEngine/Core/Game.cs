#define FULLSCREEN
#if DEBUG
using System.Runtime.InteropServices;
#endif
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Sound;
using SDL2;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Core
{
    internal static class Game
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
        public static readonly Size2D RenderSize = new(RenderWidth, RenderHeight);
        public const int NumTicksPerSecond = 20;
        private const string ScreenshotPath = @"Screenshots";

        private const float NumMillisecondsPerTick = 1_000f / NumTicksPerSecond;

        public static DateTime LogicTickTime { get; private set; }
        public static DateTime RenderTickTime { get; private set; }
        public static TimeSpan RenderTimeSinceLastFrame { get; private set; }

        private static readonly IntPtr _window;
        private static readonly IntPtr _gl;
        public static readonly GL OpenGL;
        private static IntPtr _controller;
        private static int _controllerId;

        static Game()
        {
            // SDL 2
            if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO | SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER) != 0)
            {
                Print_SDL_Error("SDL could not initialize!");
            }

            // Use OpenGL 3.3 core
            if (SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3) != 0)
            {
                Print_SDL_Error("Could not set OpenGL's major version!");
            }
            if (SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 3) != 0)
            {
                Print_SDL_Error("Could not set OpenGL's minor version!");
            }
            if (SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE) != 0)
            {
                Print_SDL_Error("Could not set OpenGL's profile!");
            }

            // TODO: Find out why fullscreen is broken (window is gone when alt tabbing back and forth)
            _window = SDL.SDL_CreateWindow("Pokémon Game Engine", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, RenderWidth, RenderHeight,
                SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
#if FULLSCREEN
                | SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP
#endif
                );
            if (_window == IntPtr.Zero)
            {
                Print_SDL_Error("Could not create the window!");
            }

            _gl = SDL.SDL_GL_CreateContext(_window);
            if (_gl == IntPtr.Zero)
            {
                Print_SDL_Error("Could not create the OpenGL context!");
            }
            if (SDL.SDL_GL_SetSwapInterval(1) != 0)
            {
                Print_SDL_Error("Could not enable VSync!");
            }
            if (SDL.SDL_GL_MakeCurrent(_window, _gl) != 0)
            {
                Print_SDL_Error("Could not start OpenGL on the window!");
            }
            OpenGL = GL.GetApi(SDL.SDL_GL_GetProcAddress);
#if DEBUG
            unsafe
            {
                OpenGL.Enable(EnableCap.DebugOutput);
                OpenGL.DebugMessageCallback(HandleGLError, null);
            }
#endif
            OpenGL.Viewport(0, 0, RenderWidth, RenderHeight);

            CreateGLFrameBuffer();

            AttachFirstController();

            // Init SDL Audio
            SoundMixer.Init();

            // The rest inits in the top of Start()
        }

        private static uint _virtualFBO;
        private static uint _virtualFBOTexture;
        private static uint _virtualFBODepthTexture;
        private static uint _virtualFBODepthBuffer;
        private static unsafe void CreateGLFrameBuffer()
        {
            GL gl = OpenGL;
            _virtualFBO = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _virtualFBO);
            GLHelper.ActiveTexture(gl, TextureUnit.Texture0);

            // add texture attachment
            _virtualFBOTexture = GLHelper.GenTexture(gl);
            GLHelper.BindTexture(gl, _virtualFBOTexture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, RenderWidth, RenderHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _virtualFBOTexture, 0);

            // add depth texture attachment
            _virtualFBODepthTexture = GLHelper.GenTexture(gl);
            GLHelper.BindTexture(gl, _virtualFBODepthTexture);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32, RenderWidth, RenderHeight, 0, PixelFormat.DepthComponent, PixelType.Float, null);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _virtualFBODepthTexture, 0);

            // add depth buffer attachment
            _virtualFBODepthBuffer = gl.GenRenderbuffer();
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _virtualFBODepthBuffer);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, RenderWidth, RenderHeight);
            gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _virtualFBODepthBuffer);
            GLHelper.PushFrameBuffer(gl, _virtualFBO, RenderWidth, RenderHeight);
        }

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

        private static void Main()
        {
            // Init Game in the proper thread
            RenderTickTime = LogicTickTime = DateTime.Now;
            Utils.SetWorkingDirectory(string.Empty);
            _ = new Engine(); // Init game
            GUIRenderer.Instance = new();

            while (true)
            {
                DoTempTasks();
                if (HandleEvents())
                {
                    break;
                }

                DateTime now = DateTime.Now;
                DateTime prev = LogicTickTime;
                bool doTick;
                if (now <= prev)
                {
                    Console.WriteLine("Time went back!");
                    doTick = true;
                }
                else
                {
                    doTick = (now - prev).TotalMilliseconds >= NumMillisecondsPerTick;
                }
                if (doTick)
                {
                    LogicTickTime = now;
                    DoLogicTick();
                }

                now = DateTime.Now;
                prev = RenderTickTime;
                RenderTimeSinceLastFrame = now <= prev ? TimeSpan.Zero : now - prev;
                RenderTickTime = now;
                DoRenderTick();
            }

            // Quitting
            GameExit();
        }

        private static bool HandleEvents()
        {
            while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                    {
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
                            case SDL.SDL_Keycode.SDLK_F12: // Take a screenshot
                            {
                                string path = Path.Combine(ScreenshotPath, string.Format("Screenshot_{0:MM-dd-yyyy_HH-mm-ss-fff}.png", DateTime.Now));
                                path = GLTextureUtils.SaveScreenTextureAsImage(OpenGL, _virtualFBOTexture, RenderWidth, RenderHeight, path);
                                Console.WriteLine("Screenshot saved to {0}", path);
                                break;
                            }
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

        private static void DoLogicTick()
        {
            InputManager.LogicTick();
            Engine.Instance.LogicTick();
        }
        private static void DoRenderTick()
        {
            // Set up virtual screen's frame buffer
            GL gl = OpenGL;
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _virtualFBO);
            gl.Viewport(0, 0, RenderWidth, RenderHeight);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            // Render game to the virtual screen
            Engine.Instance.RenderTick(gl
#if DEBUG
                //, InputManager.Debug_GetKeys(), Font.Default, FontColors.DefaultWhite_DarkerOutline_I
                //, InputManager.Debug_GetKeys(), Font.DefaultSmall, FontColors.DefaultWhite_DarkerOutline_I
                , null, null, null
#endif
                );

            // Draw rendered frame to the actual screen's framebuffer
            SDL.SDL_GetWindowSize(_window, out int w, out int h);
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _virtualFBO);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            gl.DrawBuffer(DrawBufferMode.Back); // The default frame buffer isn't "ColorAttachment0", it's "Back" instead
            gl.BlitFramebuffer(0, 0, RenderWidth, RenderHeight, 0, 0, w, h, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            // Present to window
            SDL.SDL_GL_SwapWindow(_window);
        }

        private static void Print_SDL_Error(string error)
        {
            error = string.Format("{2}{0}SDL Error: \"{1}\"", Environment.NewLine, SDL.SDL_GetError(), error);
            Console.WriteLine(error);
            throw new Exception(error);
        }
#if DEBUG
        private static void HandleGLError(GLEnum _, GLEnum type, int id, GLEnum severity, int length, IntPtr message, IntPtr __)
        {
            if (severity == GLEnum.DebugSeverityNotification)
            {
                return;
            }
            string msg = Marshal.PtrToStringAnsi(message, length);
            Console.WriteLine("----- GL Error -----{0}Message: \"{1}\"{0}Type: {2}{0}Id: {3}{0}Severity: {4}{0}-----", Environment.NewLine, msg, type, id, severity);
            ;
        }
#endif

        private static void GameExit()
        {
            SoundMixer.DeInit();

            GL gl = OpenGL;
            gl.DeleteFramebuffer(_virtualFBO);
            gl.DeleteTexture(_virtualFBOTexture);
            gl.DeleteTexture(_virtualFBODepthTexture);
            gl.DeleteRenderbuffer(_virtualFBODepthBuffer);
            // TODO: Other imgs, objs, tilesets, etc. Need a callback for game exit
            GUIRenderer.Instance.GameExit(gl);
            GUIString.GameExit(gl);
            AssimpLoader.GameExit();
            Font.GameExit(gl);

            SDL.SDL_GameControllerClose(_controller);
            SDL.SDL_GL_DeleteContext(_gl);
            SDL.SDL_DestroyWindow(_window);
            SDL.SDL_Quit();
        }
    }
}
