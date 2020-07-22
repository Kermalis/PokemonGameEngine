using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class SelectSomethingDialog : Window, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable Items { get; }
        private object _selectedItem;
        public object SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        // This constructor is so xaml compiles
        public SelectSomethingDialog()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }
        internal SelectSomethingDialog(IEnumerable items)
        {
            Items = items;

            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }

        public void OkClicked()
        {
            Close(SelectedItem);
        }
        public void CancelClicked()
        {
            Close(null);
        }
    }
}
