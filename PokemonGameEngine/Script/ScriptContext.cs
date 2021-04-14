using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext : IDisposable
    {
        private readonly EndianBinaryReader _reader;
        private readonly Stack<long> _callStack = new Stack<long>();
        private bool _isDisposed;
        private ushort _delay;
        private Obj _waitMovementObj;
        private bool _waitMessageBox;
        private bool _waitBattle;

        private StringPrinter _stringPrinter;
        private Window _messageBox;

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
                if (!_stringPrinter.IsDone)
                {
                    stopRunning = true;
                }
                else if (update)
                {
                    _waitMessageBox = false;
                }
            }
            if (_waitBattle)
            {
                if (!Game.Instance.IsOnOverworld)
                {
                    stopRunning = true;
                }
                else if (update)
                {
                    _waitBattle = false;
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
