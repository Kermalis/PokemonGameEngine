using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext
    {
        private void CreateMessageBox(string text)
        {
            if (_messageBox is null)
            {
                _messageBox = Window.CreateStandardMessageBox(Colors.White4);
            }
            _stringPrinter?.Delete();
            _stringPrinter = StringPrinter.CreateStandardMessageBox(_messageBox, text, Font.Default, FontColors.DefaultDarkGray_I);
        }
        private string ReadString()
        {
            uint textOffset = _reader.ReadUInt32();
            return ReadString(textOffset);
        }
        private string ReadString(uint textOffset)
        {
            long returnOffset = _reader.BaseStream.Position;
            string text = _reader.ReadStringNullTerminated(textOffset);
            _reader.BaseStream.Position = returnOffset;
            return text;
        }

        private void MessageCommand()
        {
            string text = ReadString();
            CreateMessageBox(text);
        }
        private void AwaitMessageCommand(bool complete)
        {
            _waitMessageBox = true;
            _waitForMessageCompletion = complete;
        }
        private void CloseMessageCommand()
        {
            _stringPrinter.Delete();
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
