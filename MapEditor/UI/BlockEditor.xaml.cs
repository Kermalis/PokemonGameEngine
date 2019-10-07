using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Kermalis.MapEditor.UI
{
    public sealed class BlockEditor : Window
    {
        public BlockEditor()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }
    }
}
