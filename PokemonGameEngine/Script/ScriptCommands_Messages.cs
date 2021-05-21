using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.Render;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext
    {
        private void MessageCommand()
        {
            uint textOffset = _reader.ReadUInt32();
            long returnOffset = _reader.BaseStream.Position;
            string text = _reader.ReadStringNullTerminated(textOffset);
            _reader.BaseStream.Position = returnOffset;
            if (_messageBox is null)
            {
                _messageBox = new Window(0.00f, 0.79f, 1f, 0.17f, RenderUtils.Color(255, 255, 255, 255));
            }
            _stringPrinter?.Close();
            _stringPrinter = new StringPrinter(_messageBox, text, 0.05f, 0.01f, Font.Default, Font.DefaultDarkGray_I);
        }
        private void AwaitMessageCommand(bool complete)
        {
            _waitMessageBox = true;
            _waitMessageComplete = complete;
        }
        private void CloseMessageCommand()
        {
            // Set to false, since it's possible awaitmessage completely passes the "should stop running" check
            // Avoids a crash
            _waitMessageBox = false;
            _stringPrinter.Close();
            _stringPrinter = null;
            _messageBox.Close();
            _messageBox = null;
        }

        private void CloseChoices()
        {
            _multichoiceWindow.Close();
            _multichoiceWindow = null;
            _multichoice.Dispose();
            _multichoice = null;
        }

        private void YesNoAction(bool value)
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = (short)(value ? 1 : 0);
            CloseChoices();
        }
        /*private void MultichoiceAction(short value)
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = value;
            CloseChoices();
        }*/
        private void YesNoChoiceCommand()
        {
            TextGUIChoices.CreateStandardYesNoChoices(YesNoAction, out _multichoice, out _multichoiceWindow);
        }
    }
}
