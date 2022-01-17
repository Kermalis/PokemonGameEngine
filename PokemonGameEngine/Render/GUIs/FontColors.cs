using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal static class FontColors
    {
        public static Vector4[] DefaultDisabled { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(133, 133, 141), Colors.V4FromRGB(58, 50, 50) };
        // Standard colors
        public static Vector4[] DefaultBlack_I { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(15, 25, 30), Colors.V4FromRGB(170, 185, 185) };
        public static Vector4[] DefaultBlue_I { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(0, 110, 250), Colors.V4FromRGB(120, 185, 230) };
        public static Vector4[] DefaultBlue_O { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(115, 148, 255), Colors.V4FromRGB(0, 0, 214) };
        public static Vector4[] DefaultCyan_O { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(50, 255, 255), Colors.V4FromRGB(0, 90, 140) };
        public static Vector4[] DefaultDarkGray_I { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(90, 82, 82), Colors.V4FromRGB(165, 165, 173) };
        public static Vector4[] DefaultRed_I { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(230, 30, 15), Colors.V4FromRGB(250, 170, 185) };
        public static Vector4[] DefaultRed_O { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(255, 50, 50), Colors.V4FromRGB(110, 0, 0) };
        public static Vector4[] DefaultRed_Lighter_O { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(255, 115, 115), Colors.V4FromRGB(198, 0, 0) };
        public static Vector4[] DefaultYellow_O { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(255, 224, 22), Colors.V4FromRGB(188, 165, 16) };
        public static Vector4[] DefaultWhite_I { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(239, 239, 239), Colors.V4FromRGB(132, 132, 132) };
        public static Vector4[] DefaultWhite_DarkerOutline_I { get; } = new Vector4[] { Colors.Transparent, Colors.V4FromRGB(250, 250, 250), Colors.V4FromRGB(80, 80, 80) };

#if DEBUG
        public static Vector4[] DefaultDebug { get; } = new Vector4[] { Colors.Red4, Colors.Green4, Colors.Blue4 };
#endif
    }
}
