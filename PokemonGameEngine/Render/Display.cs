using Kermalis.PokemonGameEngine.Render.OpenGL;
using SDL2;
using Silk.NET.OpenGL;
using System;
using System.IO;
using System.Runtime.InteropServices;
#if DEBUG
using Kermalis.PokemonGameEngine.Debug;
#endif

namespace Kermalis.PokemonGameEngine.Render
{
    internal static class Display
    {
        private const string SCREENSHOT_PATH = @"Screenshots";

        // A block is 16x16 pixels (2x2 tiles, and a tile is 8x8 pixels)
        // You can have different sized blocks and tiles if you wish, but this table is demonstrating defaults
        // GB/GBC        - 160 x 144 resolution (10:9) - 10 x  9   blocks
        // GBA           - 240 x 160 resolution ( 3:2) - 15 x 10   blocks
        // NDS           - 256 x 192 resolution ( 4:3) - 16 x 12   blocks
        // 3DS (Lower)   - 320 x 240 resolution ( 4:3) - 20 x 15   blocks
        // 3DS (Upper)   - 400 x 240 resolution ( 5:3) - 25 x 15   blocks
        // Default below - 384 x 216 resolution (16:9) - 24 x 13.5 blocks
        public const int RenderWidth = 384;
        public const int RenderHeight = 216;
        public static readonly Size2D RenderSize = new(RenderWidth, RenderHeight);

        private static readonly IntPtr _window;
        private static readonly IntPtr _gl;

        public static readonly GL OpenGL;
        public static float DeltaTime;

        private static FrameBuffer _virtualFBO;

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
        }

        public static void Init()
        {
            OpenGL.Viewport(0, 0, RenderWidth, RenderHeight);
            _virtualFBO = FrameBuffer.CreateWithColorAndDepth(RenderSize);
            _virtualFBO.Push();
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

            // Set up virtual screen's framebuffer
            GL gl = OpenGL;
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _virtualFBO.Id);
            gl.Viewport(0, 0, RenderWidth, RenderHeight);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

            return false;
        }
        public static void PresentFrame()
        {
            // Draw rendered frame to the actual screen's framebuffer
            GL gl = OpenGL;
            SDL.SDL_GetWindowSize(_window, out int w, out int h);
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _virtualFBO.Id);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            gl.DrawBuffer(DrawBufferMode.Back); // The default frame buffer isn't "ColorAttachment0", it's "Back" instead

            // Maintain aspect ratio of the virtual screen
            float ratioX = w / (float)RenderWidth;
            float ratioY = h / (float)RenderHeight;
            float ratio = ratioX < ratioY ? ratioX : ratioY;
            int dstX = (int)((w - (RenderWidth * ratio)) * 0.5f);
            int dstY = (int)((h - (RenderHeight * ratio)) * 0.5f);
            int dstW = (int)(RenderWidth * ratio);
            int dstH = (int)(RenderHeight * ratio);

            gl.ClearColor(Colors.Black3);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            gl.BlitFramebuffer(0, 0, RenderWidth, RenderHeight, dstX, dstY, dstX + dstW, dstY + dstH, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            // Present to window
            SDL.SDL_GL_SwapWindow(_window);
        }

        public static void SaveScreenshot()
        {
            string path = Path.Combine(SCREENSHOT_PATH, string.Format("Screenshot_{0:MM-dd-yyyy_HH-mm-ss-fff}.png", DateTime.Now));
            path = GLTextureUtils.SaveScreenTextureAsImage(OpenGL, _virtualFBO.ColorTexture, RenderWidth, RenderHeight, path);
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
            _virtualFBO.Delete();
            SDL.SDL_GL_DeleteContext(_gl);
            SDL.SDL_DestroyWindow(_window);
            SDL.SDL_Quit();
        }
    }
}
