using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class NewAssetDialog : Window, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private readonly IdList _idList;
        private readonly TextBox _textBox;

        private bool _okEnabled;
        public bool OkEnabled
        {
            get => _okEnabled;
            private set
            {
                if (value != _okEnabled)
                {
                    _okEnabled = value;
                    OnPropertyChanged(nameof(OkEnabled));
                }
            }
        }

        // This constructor is so xaml compiles
        public NewAssetDialog()
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }
        internal NewAssetDialog(IdList idList)
        {
            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _idList = idList;
            _textBox = this.FindControl<TextBox>("TextBox");
            _textBox.PropertyChanged += TextBox_PropertyChanged;
        }

        public void OkClicked()
        {
            Close(_textBox.Text);
        }
        public void CancelClicked()
        {
            Close(null);
        }

        private void TextBox_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name != nameof(TextBox.Text))
            {
                return;
            }
            string text = (string)e.NewValue;
            OkEnabled = IsValidName(text);
        }

        private bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && !Utils.InvalidFileNameRegex.IsMatch(name) && _idList[name] == -1;
        }
    }
}
