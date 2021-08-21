using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.R3D;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed partial class BattleGUI
    {
        private readonly TaskList _tasks = new();
        private readonly TaskList _renderTasks = new();

        #region Messages

        private const int AutoAdvanceTicks = Game.NumTicksPerSecond * 3; // 3 seconds

        private sealed class TaskData_PrintMessage
        {
            public Action OnFinished;
        }

        public void ClearMessage()
        {
            _stringPrinter?.Delete();
            _stringPrinter = null;
        }
        private void SetMessage_Internal(string str, Action onRead, BackTaskAction a)
        {
            _stringPrinter?.Delete();
            _stringPrinter = null;
            if (str is not null)
            {
                _stringPrinter = StringPrinter.CreateStandardMessageBox(_stringWindow, str, Font.Default, FontColors.DefaultWhite_I);
                var data = new TaskData_PrintMessage
                {
                    OnFinished = onRead
                };
                _tasks.Add(a, int.MaxValue, data: data);
            }
        }
        private void SetMessage(string str, Action onRead)
        {
            SetMessage_Internal(str, onRead, Task_ReadOutMessage);
        }
        private void SetStaticMessage(string str, Action onRead)
        {
            SetMessage_Internal(str, onRead, Task_ReadOutStaticMessage);
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
                    _tasks.RemoveAndDispose(task);
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
                _tasks.RemoveAndDispose(task);
            }
        }

        #endregion

        #region Camera Motion

        private static readonly PositionRotation _defaultPosition = new(new Vector3(7f, 7f, 15f), 0, 13, -22); // battle using bw2 matrix
        //private static readonly PositionRotation _defaultPosition = new(new Vector3(6.5f, 9.5f, 14f), 0, 18, -22); // battle 1:1, 35fov

        private sealed class TaskData_MoveCamera
        {
            public const int Tag = 0x2192A9;

            public Action OnFinished;
            public IPositionRotationAnimator Animator;

            public TaskData_MoveCamera(Camera c, PositionRotation to, Action onFinished, double milliseconds)
            {
                OnFinished = onFinished;
                Animator = new PositionRotationAnimator(new PositionRotation(c.PR), to, milliseconds);
            }
            public TaskData_MoveCamera(Camera c, PositionRotation to, Action onFinished, double posMilliseconds, double rotMilliseconds)
            {
                OnFinished = onFinished;
                Animator = new PositionRotationAnimatorSplit(new PositionRotation(c.PR), to, posMilliseconds, rotMilliseconds);
            }
        }

        private void MoveCameraToDefaultPosition(Action onFinished, double milliseconds = 500)
        {
            CreateCameraMotionTask(_defaultPosition, onFinished, milliseconds: milliseconds);
        }
        private void CreateCameraMotionTask(TaskData_MoveCamera data)
        {
            _renderTasks.Add(Task_CameraMotion, int.MaxValue, data: data, tag: TaskData_MoveCamera.Tag);
        }
        private void CreateCameraMotionTask(PositionRotation to, Action onFinished, double milliseconds = 500)
        {
            CreateCameraMotionTask(new TaskData_MoveCamera(_camera, to, onFinished, milliseconds));
        }
        private void CreateCameraMotionTask(PositionRotation to, Action onFinished, double posMilliseconds, double rotMilliseconds)
        {
            CreateCameraMotionTask(new TaskData_MoveCamera(_camera, to, onFinished, posMilliseconds, rotMilliseconds));
        }

        private void Task_CameraMotion(BackTask task)
        {
            var data = (TaskData_MoveCamera)task.Data;
            if (data.Animator.Update(_camera.PR))
            {
                _renderTasks.RemoveAndDispose(task);
                data.OnFinished?.Invoke();
            }
        }

        #endregion

        #region Trainer Sprite

        private sealed class SpriteData_TrainerGoAway
        {
            public const int Tag = 0xDEE2;

            public TimeSpan Cur;
            public readonly TimeSpan End;
            public readonly int StartX;

            public SpriteData_TrainerGoAway(double milliseconds, int startX)
            {
                Cur = new TimeSpan();
                End = TimeSpan.FromMilliseconds(milliseconds);
                StartX = startX;
            }
        }

        private void Sprite_TrainerGoAway(Sprite sprite)
        {
            var data = (SpriteData_TrainerGoAway)sprite.Data;
            double progress = Renderer.GetAnimationProgress(data.End, ref data.Cur);
            int x = (int)(progress * (GLHelper.CurrentWidth - data.StartX));
            sprite.Pos.X = data.StartX + x;
            if (progress >= 1)
            {
                _sprites.RemoveAndDispose(sprite); // Dispose callback and delete image texture
            }
        }

        #endregion
    }
}
