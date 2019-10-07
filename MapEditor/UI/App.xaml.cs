using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop: desktop.MainWindow = new MainWindow(); break;
                default: throw new PlatformNotSupportedException();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
