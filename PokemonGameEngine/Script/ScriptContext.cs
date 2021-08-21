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
        private static readonly List<ScriptContext> _allScripts = new();

        private readonly EndianBinaryReader _reader;
        private readonly Stack<long> _callStack = new();
        private bool _isDisposed;
        private ushort _delay;
        private Obj _waitMovementObj;

        private bool _waitMessageBox;
        private bool _waitMessageComplete;
        private Action _onWaitMessageFinished;

        private bool _waitReturnToField;
        private Action _onWaitReturnToFieldFinished;
        private bool _waitCry;

        private StringPrinter _stringPrinter;
        private Window _messageBox;

        private TextGUIChoices _multichoice;
        private Window _multichoiceWindow;

        public ScriptContext(EndianBinaryReader r)
        {
            _reader = r;
            _allScripts.Add(this);
        }

        private bool ShouldLeaveLogicTick(bool update)
        {
            if (_isDisposed)
            {
                return true;
            }
            bool stopRunning = _waitCry; // Wait cry needs no logic
            if (_delay != 0)
            {
                if (update)
                {
                    _delay--;
                }
                stopRunning = true;
            }
            if (_waitMovementObj is not null)
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
                if (_stringPrinter is not null && (_waitMessageComplete ? !_stringPrinter.IsDone : !_stringPrinter.IsEnded))
                {
                    stopRunning = true;
                }
                else if (update)
                {
                    _waitMessageBox = false;
                    _onWaitMessageFinished?.Invoke();
                    _onWaitMessageFinished = null;
                    if (_isDisposed)
                    {
                        return true;
                    }
                }
            }
            if (_multichoiceWindow is not null)
            {
                stopRunning = true;
                if (update)
                {
                    int s = _multichoice.Selected;
                    _multichoice.HandleInputs();
                    if (_multichoiceWindow is not null) // Was not just closed
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
                if (!Engine.Instance.IsOnOverworld)
                {
                    stopRunning = true;
                }
                else if (update)
                {
                    _waitReturnToField = false;
                    _onWaitReturnToFieldFinished?.Invoke();
                    _onWaitReturnToFieldFinished = null;
                    if (_isDisposed)
                    {
                        return true;
                    }
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

        public static void ProcessAll()
        {
            foreach (ScriptContext ctx in _allScripts.ToArray()) // Copy the list so a script ending/starting does not crash here
            {
                ctx.LogicTick();
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _allScripts.Remove(this);
                _reader.Dispose();
                _onWaitMessageFinished = null;
                _onWaitReturnToFieldFinished = null;
            }
        }
    }
}
