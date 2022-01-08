using Kermalis.PokemonGameEngine.Render.OpenGL;
using SDL2;
using Silk.NET.OpenGL;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
#if DEBUG
using Kermalis.PokemonGameEngine.Debug;
#endif

namespace Kermalis.PokemonGameEngine.Render
{
    internal static class Display
    {
        private const string WINDOW_TITLE = "Pokémon Game Engine";
        private const int DEFAULT_WINDOW_WIDTH = 1200; // 16:9
        private const int DEFAULT_WINDOW_HEIGHT = 675;
        private const string SCREENSHOT_PATH = @"Screenshots";
        private static readonly bool _debugScreenshotCurrentFrameBuffer = false;

        private static readonly IntPtr _window;
        private static readonly IntPtr _gl;

        public static readonly GL OpenGL;
        public static Vec2I ViewportSize;
        public static float DeltaTime;
        public static bool ScreenshotRequested;

        static Display()
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

            _window = SDL.SDL_CreateWindow(WINDOW_TITLE, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, DEFAULT_WINDOW_WIDTH, DEFAULT_WINDOW_HEIGHT,
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
        }

        private static Vec2I GetWindowSize()
        {
            Vec2I ret;
            SDL.SDL_GetWindowSize(_window, out ret.X, out ret.Y);
            return ret;
        }
        public static void Viewport(in Rect rect)
        {
            Vec2I size = rect.GetSize();
            OpenGL.Viewport(rect.TopLeft.X, rect.TopLeft.Y, (uint)size.X, (uint)size.Y);
            ViewportSize = size;
        }
        public static Rect FitToScreen(Vec2I inSize)
        {
            // Maintain aspect ratio of inSize
            Vec2I windowSize = GetWindowSize();
            Vector2 ratios = (Vector2)windowSize / inSize;
            float ratio = ratios.X < ratios.Y ? ratios.X : ratios.Y;
            Vector2 size = inSize * ratio;
            Vector2 topLeft = (windowSize - size) * 0.5f;
            return Rect.FromSize((Vec2I)topLeft, (Vec2I)size);
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
            return false;
        }
        public static void PresentFrame()
        {
            if (ScreenshotRequested)
            {
                ScreenshotRequested = false;
                SaveScreenshot();
            }
            SDL.SDL_GL_SwapWindow(_window);
        }

        private static void SaveScreenshot()
        {
            string path = Path.Combine(SCREENSHOT_PATH, string.Format("Screenshot_{0:MM-dd-yyyy_HH-mm-ss-fff}.png", DateTime.Now));
            if (_debugScreenshotCurrentFrameBuffer)
            {
                path = GLTextureUtils.SaveReadBufferAsImage(OpenGL, ViewportSize, path);
            }
            else
            {
                OpenGL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
                path = GLTextureUtils.SaveReadBufferAsImage(OpenGL, GetWindowSize(), path);
            }
#if DEBUG
            Log.WriteLineWithTime(string.Format("Screenshot saved to {0}", path));
#endif
        }

        public static void Print_SDL_Error(string error)
        {
            error = string.Format("{2}{0}SDL Error: \"{1}\"", Environment.NewLine, SDL.SDL_GetError(), error);
#if DEBUG
            Log.WriteLineWithTime(error);
#endif
            throw new Exception(error);
        }
#if DEBUG
        // TODO: GL crash when Discord starts streaming the application
        private static void HandleGLError(GLEnum _, GLEnum type, int id, GLEnum severity, int length, IntPtr message, IntPtr __)
        {
            if (severity == GLEnum.DebugSeverityNotification)
            {
                return;
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
            SDL.SDL_GL_DeleteContext(_gl);
            SDL.SDL_DestroyWindow(_window);
            SDL.SDL_Quit();
        }
    }
}
