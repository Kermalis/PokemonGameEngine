namespace Kermalis.PokemonGameEngine.Render.Fonts
{
    internal static class FontColors
    {
        public static ColorF[] DefaultDisabled { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(133, 133, 141), ColorF.FromRGB(58, 50, 50) };
        // Standard colors
        public static ColorF[] DefaultBlack_I { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(15, 25, 30), ColorF.FromRGB(170, 185, 185) };
        public static ColorF[] DefaultBlue_I { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(0, 110, 250), ColorF.FromRGB(120, 185, 230) };
        public static ColorF[] DefaultBlue_O { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(115, 148, 255), ColorF.FromRGB(0, 0, 214) };
        public static ColorF[] DefaultCyan_O { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(50, 255, 255), ColorF.FromRGB(0, 90, 140) };
        public static ColorF[] DefaultDarkGray_I { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(90, 82, 82), ColorF.FromRGB(165, 165, 173) };
        public static ColorF[] DefaultRed_I { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(230, 30, 15), ColorF.FromRGB(250, 170, 185) };
        public static ColorF[] DefaultRed_O { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(255, 50, 50), ColorF.FromRGB(110, 0, 0) };
        public static ColorF[] DefaultRed_Lighter_O { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(255, 115, 115), ColorF.FromRGB(198, 0, 0) };
        public static ColorF[] DefaultYellow_O { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(255, 224, 22), ColorF.FromRGB(188, 165, 16) };
        public static ColorF[] DefaultWhite_I { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(239, 239, 239), ColorF.FromRGB(132, 132, 132) };
        public static ColorF[] DefaultWhite_DarkerOutline_I { get; } = new ColorF[] { Colors.Transparent, ColorF.FromRGB(250, 250, 250), ColorF.FromRGB(80, 80, 80) };

#if DEBUG
        public static ColorF[] DefaultDebug { get; } = new ColorF[] { Colors.Red, Colors.Green, Colors.Blue };
#endif
    }
}
