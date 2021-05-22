using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext : IDisposable
    {
        private readonly EndianBinaryReader _reader;
        private readonly Stack<long> _callStack = new();
        private bool _isDisposed;
        private ushort _delay;
        private Obj _waitMovementObj;

        private bool _waitMessageBox;
        private bool _waitMessageComplete;

        private bool _waitReturnToField;

        private StringPrinter _stringPrinter;
        private Window _messageBox;

        private TextGUIChoices _multichoice;
        private Window _multichoiceWindow;

        public ScriptContext(EndianBinaryReader r)
        {
            _reader = r;
        }

        private bool ShouldLeaveLogicTick(bool update)
        {
            if (_isDisposed)
            {
                return true;
            }
            bool stopRunning = false;
            if (_delay != 0)
            {
                if (update)
                {
                    _delay--;
                }
                stopRunning = true;
            }
            if (_waitMovementObj != null)
            {
                if (_waitMovementObj.IsMoving)
                {
                    stopRunning = true;
                }
                else if (update)
                {
                    _waitMovementObj = null;
                }
            }
            if (_waitMessageBox)
            {
                // If "AwaitMessage" are not the first to run this tick, they will not update and set _waitMessageBox to false
                if (_stringPrinter != null && (_waitMessageComplete ? !_stringPrinter.IsDone : !_stringPrinter.IsEnded))
                {
                    stopRunning = true;
                }
                else if (update)
                {
                    _waitMessageBox = false;
                }
            }
            if (_multichoiceWindow != null)
            {
                stopRunning = true;
                if (update)
                {
                    int s = _multichoice.Selected;
                    _multichoice.HandleInputs();
                    if (!(_multichoiceWindow is null)) // Was not just closed
                    {
                        if (s != _multichoice.Selected)
                        {
                            _multichoice.RenderChoicesOntoWindow(_multichoiceWindow);
                        }
                    }
                }
            }
            if (_waitReturnToField)
            {
                if (!Game.Instance.IsOnOverworld)
                {
                    stopRunning = true;
                }
                else if (update)
                {
                    _waitReturnToField = false;
                }
            }
            return stopRunning;
        }
        public void LogicTick()
        {
            bool update = true;
            while (!ShouldLeaveLogicTick(update))
            {
                update = false;
                RunNextCommand();
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            Game.Instance.Scripts.Remove(this);
            _reader.Dispose();
        }
    }
}
