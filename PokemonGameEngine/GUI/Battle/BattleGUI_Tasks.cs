using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.UI;
using System;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed partial class BattleGUI
    {
        #region Task classes

        private sealed class TaskData_PrintMessage
        {
            public Action OnFinished;
        }
        private sealed class SpriteData_TrainerGoAway
        {
            public const int Tag = 0xDEE;

            public TimeSpan Cur;
            public readonly TimeSpan End;
            public readonly int StartX;

            public SpriteData_TrainerGoAway(int ms, int startX)
            {
                Cur = new TimeSpan();
                End = TimeSpan.FromMilliseconds(ms);
                StartX = startX;
            }
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
                    data.OnFinished();
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
                data.OnFinished();
                _tasks.Remove(task);
            }
        }

        private void Sprite_TrainerGoAway(Sprite sprite)
        {
            var data = (SpriteData_TrainerGoAway)sprite.Data;
            double progress = Renderer.GetAnimationProgress(data.End, ref data.Cur);
            int x = (int)(progress * (Program.RenderWidth - data.StartX));
            sprite.X = data.StartX + x;
            if (progress >= 1)
            {
                sprite.Callback = null;
                _sprites.Remove(sprite);
            }
        }
    }
}
