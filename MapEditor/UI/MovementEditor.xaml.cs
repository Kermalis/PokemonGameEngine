using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using System;

namespace Kermalis.MapEditor.UI
{
    public sealed class MovementEditor : UserControl, IDisposable
    {
        private readonly MovementImage _movementImage;

        public MovementEditor()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _movementImage = this.FindControl<MovementImage>("MovementImage");
        }

        internal void SetLayout(Map.Layout layout)
        {
            _movementImage.Layout = layout;
        }

        public void Dispose()
        {
            _movementImage.Dispose();
        }
    }
}
