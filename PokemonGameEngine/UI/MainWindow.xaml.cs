using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.PokemonGameEngine.Input;

namespace Kermalis.PokemonGameEngine.UI
{
    public sealed class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            MinWidth = MainView.RenderWidth;
            MinHeight = MainView.RenderHeight;
        }

        protected override void OnKeyDown(Avalonia.Input.KeyEventArgs e)
        {
            InputManager.OnKeyDown(e, true);
            base.OnKeyDown(e);
        }
        protected override void OnKeyUp(Avalonia.Input.KeyEventArgs e)
        {
            InputManager.OnKeyDown(e, false);
            base.OnKeyUp(e);
        }
    }
}
