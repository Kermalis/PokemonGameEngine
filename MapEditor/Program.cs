﻿using Avalonia;
using Avalonia.ReactiveUI;
using Kermalis.MapEditor.UI;
using Kermalis.PokemonBattleEngine.DefaultData;
using System;

namespace Kermalis.MapEditor
{
    internal static class Program
    {
        public const string AssetPath = @"../../../../PokemonGameEngine/Assets";

        [STAThread]
        private static void Main()
        {
            PBEDefaultDataProvider.InitEngine(string.Empty);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(null);
        }
        /// <summary>This method is needed for IDE previewer infrastructure.</summary>
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                           .UsePlatformDetect()
                           .UseReactiveUI();
        }
    }
}
