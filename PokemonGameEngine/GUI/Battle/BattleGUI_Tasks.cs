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

        private const float AUTO_ADVANCE_SECONDS = 3f;

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
            _stringPrinter.Update();
            if (!_stringPrinter.IsEnded)
            {
                return;
            }

            if (_stringPrinter.IsDone || (_autoAdvanceTime += Display.DeltaTime) >= AUTO_ADVANCE_SECONDS)
            {
                var data = (TaskData_PrintMessage)task.Data;
                _autoAdvanceTime = 0f;
                data.OnFinished();
                _tasks.RemoveAndDispose(task);
            }
        }
        private void Task_ReadOutStaticMessage(BackTask task)
        {
            _stringPrinter.Update();
            if (!_stringPrinter.IsEnded)
            {
                return;
            }

            var data = (TaskData_PrintMessage)task.Data;
            data.OnFinished();
            _tasks.RemoveAndDispose(task);
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

            public TaskData_MoveCamera(Camera c, in PositionRotation to, Action onFinished, float seconds)
            {
                OnFinished = onFinished;
                Animator = new PositionRotationAnimator(c.PR, to, seconds);
            }
            public TaskData_MoveCamera(Camera c, in PositionRotation to, Action onFinished, float posSeconds, float rotSeconds)
            {
                OnFinished = onFinished;
                Animator = new PositionRotationAnimatorSplit(c.PR, to, posSeconds, rotSeconds);
            }
        }

        private void MoveCameraToDefaultPosition(Action onFinished, float seconds = 0.5f)
        {
            CreateCameraMotionTask(_defaultPosition, onFinished, seconds: seconds);
        }
        private void CreateCameraMotionTask(TaskData_MoveCamera data)
        {
            _renderTasks.Add(Task_CameraMotion, int.MaxValue, data: data, tag: TaskData_MoveCamera.Tag);
        }
        private void CreateCameraMotionTask(in PositionRotation to, Action onFinished, float seconds = 0.5f)
        {
            CreateCameraMotionTask(new TaskData_MoveCamera(_camera, to, onFinished, seconds));
        }
        private void CreateCameraMotionTask(in PositionRotation to, Action onFinished, float posSeconds, float rotSeconds)
        {
            CreateCameraMotionTask(new TaskData_MoveCamera(_camera, to, onFinished, posSeconds, rotSeconds));
        }

        private void Task_CameraMotion(BackTask task)
        {
            var data = (TaskData_MoveCamera)task.Data;
            if (data.Animator.Update(ref _camera.PR))
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

            public readonly int StartX;
            public readonly float Speed; // Pixels per second
            public float CurX;

            public SpriteData_TrainerGoAway(int startX, float speed)
            {
                StartX = startX;
                Speed = speed;
            }
        }

        private void Sprite_TrainerGoAway(Sprite sprite)
        {
            var data = (SpriteData_TrainerGoAway)sprite.Data;
            data.CurX += Display.DeltaTime * data.Speed;
            sprite.Pos.X = data.StartX + (int)data.CurX;
            if (sprite.Pos.X >= FrameBuffer.Current.Size.Width)
            {
                _sprites.RemoveAndDispose(sprite); // Dispose callback and delete image texture
            }
        }

        #endregion
    }
}
