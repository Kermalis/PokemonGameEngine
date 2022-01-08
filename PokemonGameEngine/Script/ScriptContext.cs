using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Script
{
    internal sealed partial class ScriptContext
    {
        private readonly Vec2I _viewSize;
        private readonly EndianBinaryReader _reader;
        private readonly Stack<long> _callStack = new();
        private ushort _msgScale = 1;
        private bool _isDead;

        private float _delayRemaining;

        private Obj _waitMovementObj;

        private bool _waitMessageBox;
        private bool _waitForMessageCompletion;
        private Action _onWaitMessageFinished;

        private bool _waitReturnToField;
        private Action _onWaitReturnToFieldFinished;
        private bool _waitCry;

        private StringPrinter _stringPrinter;
        private Window _messageBox;

        private TextGUIChoices _multichoice;
        private Window _multichoiceWindow;

        public ScriptContext(Vec2I viewSize, EndianBinaryReader r)
        {
            _viewSize = viewSize;
            _reader = r;
        }

        private void CheckWaitCry(ref bool isWaiting)
        {
            if (_waitCry)
            {
                isWaiting = true; // Still waiting for cry to finish
            }
        }
        private void CheckDelay(bool updateDelay, ref bool isWaiting)
        {
            if (_delayRemaining == 0f)
            {
                return; // Not waiting
            }
            if (updateDelay)
            {
                _delayRemaining -= Display.DeltaTime;
                if (_delayRemaining <= 0f)
                {
                    _delayRemaining = 0f;
                    return; // Don't wait anymore
                }
            }
            isWaiting = true; // Still have time to wait
        }
        private void CheckWaitMovement(ref bool isWaiting)
        {
            if (_waitMovementObj is null)
            {
                return; // Not waiting
            }
            if (!_waitMovementObj.IsMoving)
            {
                _waitMovementObj = null;
                return; // Just finished moving
            }
            isWaiting = true; // Still waiting for it to stop moving
        }
        private void CheckWaitMessageBox(ref bool isWaiting)
        {
            if (!_waitMessageBox)
            {
                return; // Not waiting
            }
            if (_stringPrinter is not null && (_waitForMessageCompletion ? !_stringPrinter.IsDone : !_stringPrinter.IsEnded))
            {
                isWaiting = true;
                return; // String printer is still open and the message is not done
            }
            // Message box finished
            _waitMessageBox = false;
            _onWaitMessageFinished?.Invoke();
            _onWaitMessageFinished = null;
        }
        private void CheckWaitMultichoice(ref bool isWaiting)
        {
            if (_multichoiceWindow is null)
            {
                return; // Not waiting
            }

            int s = _multichoice.Selected;
            _multichoice.HandleInputs();
            if (_multichoiceWindow is null)
            {
                return; // Window was just closed
            }

            if (s != _multichoice.Selected)
            {
                _multichoice.RenderChoicesOntoWindow(_multichoiceWindow); // Update selection if it has changed
            }
            isWaiting = true;
        }
        private void CheckWaitReturnToField(ref bool isWaiting)
        {
            if (!_waitReturnToField)
            {
                return; // Not waiting
            }
            if (!Game.Instance.IsOnOverworld)
            {
                isWaiting = true;
                return; // Still didn't return to overworld
            }
            // Returned to overworld
            _waitReturnToField = false;
            _onWaitReturnToFieldFinished?.Invoke();
            _onWaitReturnToFieldFinished = null;
        }

        private bool IsWaitingForSomething(bool updateDelay)
        {
            if (_isDead)
            {
                return true;
            }
            bool isWaiting = false;
            CheckWaitCry(ref isWaiting);
            CheckDelay(updateDelay, ref isWaiting);
            CheckWaitMovement(ref isWaiting);
            CheckWaitMessageBox(ref isWaiting);
            if (_isDead)
            {
                return true; // Callback can close the script
            }
            CheckWaitMultichoice(ref isWaiting);
            CheckWaitReturnToField(ref isWaiting);
            if (_isDead)
            {
                return true; // Callback can close the script
            }
            return isWaiting;
        }
        public void Update()
        {
            bool updateDelay = true; // Should update delay at the start of each frame but not when a new one starts
            while (!IsWaitingForSomething(updateDelay))
            {
                updateDelay = false;
                RunNextCommand();
            }

        }

        public void Delete()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            _reader.Dispose();
            _onWaitMessageFinished = null;
            _onWaitReturnToFieldFinished = null;
        }
    }
}
