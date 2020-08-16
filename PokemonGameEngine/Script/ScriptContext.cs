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
        private MessageBox _waitMessageBox;

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
            if (_delay != 0)
            {
                if (update)
                {
                    _delay--;
                }
                return true;
            }
            if (_waitMovementObj != null)
            {
                if (!_waitMovementObj.IsMoving)
                {
                    return true;
                }
                if (update)
                {
                    _waitMovementObj = null;
                }
            }
            if (_waitMessageBox != null)
            {
                if (Game.Instance.MessageBoxes.Contains(_waitMessageBox))
                {
                    return true;
                }
                if (update)
                {
                    _waitMessageBox = null;
                }
            }
            return false;
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
