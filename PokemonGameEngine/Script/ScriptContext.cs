using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.Render;
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

        public ScriptContext(EndianBinaryReader r)
        {
            _reader = r;
            _allScripts.Add(this);
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
            if (_isDisposed)
            {
                return true;
            }
            bool isWaiting = false;
            CheckWaitCry(ref isWaiting);
            CheckDelay(updateDelay, ref isWaiting);
            CheckWaitMovement(ref isWaiting);
            CheckWaitMessageBox(ref isWaiting);
            if (_isDisposed)
            {
                return true; // Callback can close the script
            }
            CheckWaitMultichoice(ref isWaiting);
            CheckWaitReturnToField(ref isWaiting);
            if (_isDisposed)
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

        public static void UpdateAll()
        {
            foreach (ScriptContext ctx in _allScripts.ToArray()) // Copy the list so a script ending/starting does not crash here
            {
                ctx.Update();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _allScripts.Remove(this);
            _reader.Dispose();
            _onWaitMessageFinished = null;
            _onWaitReturnToFieldFinished = null;
        }
    }
}
