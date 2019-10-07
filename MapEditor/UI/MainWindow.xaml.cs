using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Kermalis.MapEditor.Core;
using ReactiveUI;
using System;
using System.Reactive.Subjects;

namespace Kermalis.MapEditor.UI
{
    public sealed class MainWindow : Window
    {
        public ReactiveCommand OpenBlockEditorCommand { get; }
        private readonly Subject<bool> _openBlockEditorCanExecute;

        private readonly Map _map;

        private readonly BlocksetImage _blocksetImage;
        private readonly MapImage _mapImage;

        public MainWindow()
        {
            _openBlockEditorCanExecute = new Subject<bool>();
            OpenBlockEditorCommand = ReactiveCommand.Create(OpenBlockEditor, _openBlockEditorCanExecute);
            _openBlockEditorCanExecute.OnNext(true);

            var b = Blockset.LoadOrGet("Test");
            _map = new Map(10, 7, b.Blocks[0]);

            DataContext = this;
            AvaloniaXamlLoader.Load(this);

            _blocksetImage = this.FindControl<BlocksetImage>("BlocksImage");
            _blocksetImage.Blockset = b;
            _blocksetImage.SelectionCompleted += BlocksImage_SelectionCompleted;
            _mapImage = this.FindControl<MapImage>("MapImage");
            _mapImage.Map = _map;
        }

        private void BlocksImage_SelectionCompleted(object sender, Blockset.Block[][] e)
        {
            _mapImage.Selection = e;
        }

        private void OpenBlockEditor()
        {
            var be = new BlockEditor();
            be.Show();
            be.Closed += BlockEditor_Closed;
            _openBlockEditorCanExecute.OnNext(false);
        }
        private void BlockEditor_Closed(object sender, EventArgs e)
        {
            _openBlockEditorCanExecute.OnNext(true);
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("New");
        }
    }
}
