using Avalonia;
using Avalonia.Platform;
using System.IO;
using System.Text.RegularExpressions;

namespace Kermalis.MapEditor.Util
{
    internal static class Utils
    {
        public static IPlatformRenderInterface RenderInterface { get; } = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

        // Temporary fix for https://github.com/AvaloniaUI/Avalonia/issues/3079
        public static bool TemporaryFix_RectContains(this Rect rect, Point pos)
        {
            return pos.X >= rect.X && pos.Y >= rect.Y && pos.X < rect.X + rect.Width && pos.Y < rect.Y + rect.Height;
        }

        public static readonly Regex InvalidFileNameRegex = new Regex("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]");
    }
}
