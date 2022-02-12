using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
#if DEBUG
using Kermalis.PokemonGameEngine.Debug;
#endif

namespace Kermalis.PokemonGameEngine.Render
{
    internal static unsafe class Display
    {
        private const string WINDOW_TITLE = "Pokémon Game Engine";
        private const string SCREENSHOT_PATH = @"Screenshots";
        private const int AUTOSIZE_WINDOW_SCALE = 3;

        private static readonly Window* _window;
        private static readonly void* _glContext;

        public static readonly GL OpenGL;
        public static readonly Sdl SDL;
        public static bool AutosizeWindow = true; // Works silently with fullscreen mode
        public static Vec2I ViewportSize;
        public static Vec2I ScreenSize;
        public static Rect ScreenRect;
        public static float DeltaTime;

        static Display()
        {
            // SDL 2
            SDL = Sdl.GetApi();
            if (SDL.Init(Sdl.InitAudio | Sdl.InitVideo | Sdl.InitGamecontroller) != 0)
            {
                Print_SDL_Error("SDL could not initialize!");
            }

            // Use OpenGL 4.2 core. Required for glDrawArraysInstancedBaseInstance
            if (SDL.GLSetAttribute(GLattr.GLContextMajorVersion, 4) != 0)
            {
                Print_SDL_Error("Could not set OpenGL's major version!");
            }
            if (SDL.GLSetAttribute(GLattr.GLContextMinorVersion, 2) != 0)
            {
                Print_SDL_Error("Could not set OpenGL's minor version!");
            }
            if (SDL.GLSetAttribute(GLattr.GLContextProfileMask, (int)GLprofile.GLContextProfileCore) != 0)
            {
                Print_SDL_Error("Could not set OpenGL's profile!");
            }

            WindowFlags windowFlags = WindowFlags.WindowOpengl | WindowFlags.WindowResizable;
#if FULLSCREEN
            windowFlags |= WindowFlags.WindowFullscreenDesktop;
#endif

            _window = SDL.CreateWindow(WINDOW_TITLE, Sdl.WindowposUndefined, Sdl.WindowposUndefined, 1, 1, (uint)windowFlags);
            if (_window is null)
            {
                Print_SDL_Error("Could not create the window!");
            }

            _glContext = SDL.GLCreateContext(_window);
            if (_glContext is null)
            {
                Print_SDL_Error("Could not create the OpenGL context!");
            }
            if (SDL.GLSetSwapInterval(1) != 0)
            {
                Print_SDL_Error("Could not enable VSync!");
            }
            if (SDL.GLMakeCurrent(_window, _glContext) != 0)
            {
                Print_SDL_Error("Could not start OpenGL on the window!");
            }
            OpenGL = GL.GetApi((proc) => (nint)SDL.GLGetProcAddress(proc));
            // Default gl states:
            // DepthTest disabled
            OpenGL.Enable(EnableCap.Blend); // Blend enabled
            OpenGL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
#if DEBUG
            OpenGL.Enable(EnableCap.DebugOutput);
            OpenGL.DebugMessageCallback(HandleGLError, null);
#endif

            SetMinimumWindowSize(new Vec2I(1, 1));
        }

        private static Vec2I GetWindowSize()
        {
            var ret = new Vec2I();
            SDL.GetWindowSize(_window, ref ret.X, ref ret.Y);
            return ret;
        }
        public static void SetMinimumWindowSize(Vec2I size)
        {
            ScreenSize = size;
            SDL.SetWindowMinimumSize(_window, size.X, size.Y);
            if (AutosizeWindow)
            {
                size *= AUTOSIZE_WINDOW_SCALE;
                SDL.SetWindowSize(_window, size.X, size.Y);
            }
            SetScreenRect();
        }
        public static void Viewport(in Rect rect)
        {
            Vec2I size = rect.GetSize();
            OpenGL.Viewport(rect.TopLeft.X, rect.TopLeft.Y, (uint)size.X, (uint)size.Y);
            ViewportSize = size;
        }
        private static void SetScreenRect()
        {
            Vector2 windowSize = GetWindowSize();
            Vector2 ratios = windowSize / ScreenSize;
            float ratio = ratios.X < ratios.Y ? ratios.X : ratios.Y;
            Vector2 size = ScreenSize * ratio;
            Vector2 topLeft = (windowSize - size) * 0.5f;
            ScreenRect = Rect.FromSize((Vec2I)topLeft, (Vec2I)size);
        }

        /// <summary>Returns true if the current frame should be skipped</summary>
        public static bool PrepareFrame(ref DateTime mainLoopTime)
        {
            // Calculate delta time
            DateTime now = DateTime.Now;
            DateTime prev = mainLoopTime;
            mainLoopTime = now;
            if (now <= prev)
            {
#if DEBUG
                Log.WriteLineWithTime("Time went back!");
#endif
                DeltaTime = 0f;
                return true; // Skip current frame if time went back
            }
            else
            {
                DeltaTime = (float)(now - prev).TotalSeconds;
                if (DeltaTime > 1f)
                {
                    DeltaTime = 1f;
#if DEBUG
                    Log.WriteLineWithTime("Time between frames was longer than 1 second!");
#endif
                }
            }
            SetScreenRect();
            return false;
        }
        public static void PresentFrame()
        {
            if (InputManager.JustPressed(Key.Screenshot))
            {
                SaveScreenshot();
            }
            OpenGL.BindFramebuffer(FramebufferTarget.Framebuffer, 0); // Rebind default FBO. Streaming with many apps require this bound before swap
            SDL.GLSwapWindow(_window);
        }
        /// <summary>Renders a texture on top of the screen</summary>
        public static void RenderToScreen(uint texture)
        {
            GL gl = OpenGL;
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Viewport(ScreenRect);

            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, texture);
            EntireScreenTextureShader.Instance.Use(gl);
            RectMesh.Instance.Render(gl);
        }

        private static void SaveScreenshot()
        {
            string path = Path.Combine(SCREENSHOT_PATH, string.Format("Screenshot_{0:MM-dd-yyyy_HH-mm-ss-fff}.png", DateTime.Now));
            OpenGL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            path = GLTextureUtils.SaveReadBufferAsImage(OpenGL, GetWindowSize(), path);
#if DEBUG
            Log.WriteLineWithTime(string.Format("Screenshot saved to {0}", path));
#endif
        }

        public static void Print_SDL_Error(string error)
        {
            error = string.Format("{2}{0}SDL Error: \"{1}\"", Environment.NewLine, SDL.GetErrorS(), error);
#if DEBUG
            Log.WriteLineWithTime(error);
#endif
            throw new Exception(error);
        }
#if DEBUG
        private static void HandleGLError(GLEnum _, GLEnum type, int id, GLEnum severity, int length, IntPtr message, IntPtr __)
        {
            if (severity == GLEnum.DebugSeverityNotification)
            {
                return;
            }
            // GL_INVALID_ENUM error generated. Operation is not valid from the core profile.
            if (id == 1280)
            {
                return; // Ignore legacy profile func warnings. I don't use any legacy functions, but streaming apps may attempt to when hooking in
            }
            // Pixel-path performance warning: Pixel transfer is synchronized with 3D rendering.
            if (id == 131154)
            {
                return; // Ignore NVIDIA driver warning. Happens when taking a screenshot with the entire screen
            }
            // Program/shader state performance warning: Vertex shader in program {num} is being recompiled based on GL state.
            if (id == 131218)
            {
                return; // Ignore NVIDIA driver warning. Not sure what causes it and neither is Google
            }
            string msg = Marshal.PtrToStringAnsi(message, length);
            Log.WriteLineWithTime("GL Error:");
            Log.ModifyIndent(+1);
            Log.WriteLine(string.Format("Message: \"{0}\"", msg));
            Log.WriteLine(string.Format("Type: \"{0}\"", type));
            Log.WriteLine(string.Format("Id: \"{0}\"", id));
            Log.WriteLine(string.Format("Severity: \"{0}\"", severity));
            Log.ModifyIndent(-1);
            ;
        }
#endif

        public static void Quit()
        {
            SDL.GLDeleteContext(_glContext);
            SDL.DestroyWindow(_window);
            SDL.Quit();
        }
    }
}
