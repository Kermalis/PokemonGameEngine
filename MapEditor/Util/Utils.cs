using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using System.IO;
using System.Text.RegularExpressions;

namespace Kermalis.MapEditor.Util
{
    internal static class Utils
    {
        public static IPlatformRenderInterface RenderInterface { get; } = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

        // Temporary fix for https://github.com/AvaloniaUI/Avalonia/issues/3079
        public static bool TemporaryFix_PointerInControl(this Rect rect, Point pos)
        {
            double x = pos.X;
            double y = pos.Y;
            return x >= 0 && x < rect.Width && y >= 0 && y < rect.Height;
        }
        // Temporary fix for https://github.com/AvaloniaUI/Avalonia/issues/2975
        public static void TemporaryFix_Activate(this Window window)
        {
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }
            window.Activate();
        }
        public static void ForceRedraw(this ComboBox c)
        {
            IBrush old = c.Background;
            c.Background = old.Equals(Brushes.AliceBlue) ? Brushes.AntiqueWhite : Brushes.AliceBlue;
            c.Background = old;
        }

        public static readonly Regex InvalidFileNameRegex = new Regex("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]");
    }
}
