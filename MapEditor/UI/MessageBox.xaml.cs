using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

namespace Kermalis.MapEditor.UI
{
    // https://stackoverflow.com/questions/55706291/how-to-show-a-message-box-in-avaloniaui-beta
    public sealed class MessageBox : Window
    {
        public enum MessageBoxButtons
        {
            Ok,
            OkCancel,
            YesNo,
            YesNoCancel
        }
        public enum MessageBoxResult
        {
            Ok,
            Cancel,
            Yes,
            No
        }

        public MessageBox()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static Task<MessageBoxResult> Show(string text, string title, MessageBoxButtons buttons, Window owner = null)
        {
            var msgbox = new MessageBox { Title = title };
            msgbox.FindControl<TextBlock>("Text").Text = text;
            StackPanel buttonPanel = msgbox.FindControl<StackPanel>("Buttons");

            MessageBoxResult ret = MessageBoxResult.Ok;

            void AddButton(string caption, MessageBoxResult r, bool def = false)
            {
                var b = new Button { Content = caption };
                b.Click += (_, __) =>
                {
                    ret = r;
                    msgbox.Close();
                };
                buttonPanel.Children.Add(b);
                if (def)
                {
                    ret = r;
                }
            }

            if (buttons == MessageBoxButtons.Ok || buttons == MessageBoxButtons.OkCancel)
            {
                AddButton("Ok", MessageBoxResult.Ok, def: true);
            }
            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel)
            {
                AddButton("Yes", MessageBoxResult.Yes);
                AddButton("No", MessageBoxResult.No, def: true);
            }
            if (buttons == MessageBoxButtons.OkCancel || buttons == MessageBoxButtons.YesNoCancel)
            {
                AddButton("Cancel", MessageBoxResult.Cancel, def: true);
            }

            var tcs = new TaskCompletionSource<MessageBoxResult>();
            msgbox.Closed += delegate { tcs.TrySetResult(ret); };
            if (owner != null)
            {
                msgbox.ShowDialog(owner);
            }
            else
            {
                msgbox.Show();
            }
            return tcs.Task;
        }
    }
}
