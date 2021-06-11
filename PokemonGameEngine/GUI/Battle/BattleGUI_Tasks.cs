using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed partial class BattleGUI
    {
        #region Task classes

        private sealed class TaskData_PrintMessage
        {
            public Action OnFinished;
        }

        #endregion

        private readonly TaskList _tasks = new();

        public void ClearMessage()
        {
            _stringPrinter?.Close();
            _stringPrinter = null;
        }
        private void AddMessage(string str, Action onRead)
        {
            _stringPrinter?.Close();
            _stringPrinter = null;
            if (str is not null)
            {
                _stringPrinter = new StringPrinter(_stringWindow, str, 0.1f, 0.01f, Font.Default, Font.DefaultWhite_I);
                _pauseBattleThread = true;
                var data = new TaskData_PrintMessage
                {
                    OnFinished = onRead
                };
                _tasks.Add(Task_ReadOutMessage, int.MaxValue, data: data);
            }
        }
        private void AddStaticMessage(string str, Action onRead)
        {
            _stringPrinter?.Close();
            _stringPrinter = null;
            if (str is not null)
            {
                _stringPrinter = new StringPrinter(_stringWindow, str, 0.1f, 0.01f, Font.Default, Font.DefaultWhite_I);
                var data = new TaskData_PrintMessage
                {
                    OnFinished = onRead
                };
                _tasks.Add(Task_ReadOutStaticMessage, int.MaxValue, data: data);
            }
        }

        private void Task_ReadOutMessage(BackTask task)
        {
            _stringPrinter.LogicTick();
            if (_stringPrinter.IsEnded)
            {
                if (_stringPrinter.IsDone || ++_autoAdvanceTimer >= AutoAdvanceTicks)
                {
                    var data = (TaskData_PrintMessage)task.Data;
                    _autoAdvanceTimer = 0;
                    data.OnFinished.Invoke();
                    _tasks.Remove(task);
                }
            }
        }
        private void Task_ReadOutStaticMessage(BackTask task)
        {
            _stringPrinter.LogicTick();
            if (_stringPrinter.IsEnded)
            {
                var data = (TaskData_PrintMessage)task.Data;
                data.OnFinished.Invoke();
                _tasks.Remove(task);
            }
        }

        private void Sprite_TrainerGoAway(Sprite sprite)
        {
            if ((sprite.X += Program.RenderWidth / 64) >= Program.RenderWidth)
            {
                sprite.Callback = null;
                _sprites.Remove(sprite);
            }
        }
    }
}
