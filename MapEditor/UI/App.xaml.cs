using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.UI.Models;
using System;

namespace Kermalis.MapEditor.UI
{
    public sealed class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Selection.CreateBrushes();
            ConnectionModel.CreateBrush();
            StandardGrid.CreatePen();
            switch (ApplicationLifetime)
            {
                case null: break;
                case IClassicDesktopStyleApplicationLifetime desktop: desktop.MainWindow = new MainWindow(); break;
                default: throw new PlatformNotSupportedException();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
