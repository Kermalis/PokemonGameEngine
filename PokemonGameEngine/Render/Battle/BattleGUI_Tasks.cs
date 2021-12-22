using Kermalis.PokemonBattleEngine.Packets;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.R3D;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed partial class BattleGUI
    {
        private readonly TaskList _tasks = new();

        #region Messages

        private const float AUTO_ADVANCE_SECONDS = 3f;

        private sealed class TaskData_PrintMessage
        {
            public Action OnFinished;
        }

        private void ClearMessage()
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
                _stringPrinter = StringPrinter.CreateStandardMessageBox(_stringWindow, str, Font.Default, FontColors.DefaultWhite_I, RenderSize);
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
                _autoAdvanceTime = 0f;
                var data = (TaskData_PrintMessage)task.Data;
                _tasks.RemoveAndDispose(task);
                data.OnFinished();
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
            _tasks.RemoveAndDispose(task);
            data.OnFinished();
        }

        #endregion

        #region Camera Motion

        private const float CAM_SPEED_DEFAULT = 0.5f;

        private static readonly PositionRotation _defaultPosition = new(new Vector3(7f, 7f, 15f), new Rotation(-22, 13, 0)); // battle using bw2 matrix
        //private static readonly PositionRotation _defaultPosition = new(new Vector3(6.5f, 9.5f, 14f), new Rotation(-22, 18, 0)); // battle 1:1, 35fov

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

        private void CreateCameraMotionTask(TaskData_MoveCamera data)
        {
            _tasks.Add(Task_CameraMotion, int.MaxValue, data: data, tag: TaskData_MoveCamera.Tag);
        }
        private void CreateCameraMotionTask(in PositionRotation to, Action onFinished, float seconds)
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
                _tasks.RemoveAndDispose(task);
                data.OnFinished?.Invoke();
            }
        }

        #endregion

        #region Trainer Sprite

        private sealed class TaskData_TrainerReveal
        {
            public const float DELAY = 0.75f; // In seconds
            public const float SPEED = 1.00f;

            public float DelayProgress;
            public float ColorProgress;
        }
        private sealed class TaskData_TrainerGoAway
        {
            public readonly float StartZ;
            public readonly float EndZ;
            public float Progress;

            public TaskData_TrainerGoAway(float startZ)
            {
                StartZ = startZ;
                EndZ = startZ - 7.5f;
            }
        }

        private void Task_TrainerReveal(BackTask task)
        {
            var data = (TaskData_TrainerReveal)task.Data;
            // Update delay first
            if (data.DelayProgress < 1f)
            {
                data.DelayProgress += Display.DeltaTime;
                if (data.DelayProgress < 1f)
                {
                    return;
                }
                data.ColorProgress = (data.DelayProgress - 1f) * TaskData_TrainerReveal.SPEED;
                data.DelayProgress = 1f;
            }
            else
            {
                data.ColorProgress += Display.DeltaTime * TaskData_TrainerReveal.SPEED;
            }
            if (data.ColorProgress >= 1f)
            {
                _trainerSprite.MaskColorAmt = 0f;
                _trainerSprite.AnimImage.IsPaused = false;
                _tasks.RemoveAndDispose(task);
                return;
            }
            _trainerSprite.MaskColorAmt = 1f - data.ColorProgress;
        }
        private void Task_TrainerGoAway(BackTask task)
        {
            var data = (TaskData_TrainerGoAway)task.Data;
            data.Progress += Display.DeltaTime;
            if (data.Progress >= 1f)
            {
                _trainerSprite.IsVisible = false;
                _tasks.RemoveAndDispose(task); // Don't delete sprite here since we can reuse it for taunts in-battle
                return;
            }
            _trainerSprite.Pos.Z = Utils.Lerp(data.StartZ, data.EndZ, data.Progress);
            _trainerSprite.Opacity = 1f - data.Progress;
        }

        #endregion

        #region Pokemon Sprite

        private sealed class TaskData_ChangeSprite
        {
            public readonly IPBEPacket Packet;
            public Action<BattlePokemon> Reveal;
            public readonly BattlePokemon Pkmn;
            public float Progress;

            public TaskData_ChangeSprite(IPBEPacket packet, Action<BattlePokemon> reveal, BattlePokemon pkmn)
            {
                Packet = packet;
                Reveal = reveal;
                Pkmn = pkmn;
            }
        }

        private void Task_ChangeSprite_Start(BackTask task)
        {
            var data = (TaskData_ChangeSprite)task.Data;
            data.Progress += Display.DeltaTime;
            if (data.Progress >= 1f)
            {
                data.Progress = 0f;
                data.Pkmn.Pos.Sprite.PixelateAmt = 1f;
                data.Reveal(data.Pkmn);
                data.Reveal = null;
                task.Action = Task_ChangeSprite_End;
                return;
            }

            data.Pkmn.Pos.Sprite.PixelateAmt = data.Progress;
        }
        private void Task_ChangeSprite_End(BackTask task)
        {
            var data = (TaskData_ChangeSprite)task.Data;
            data.Progress += Display.DeltaTime;
            if (data.Progress >= 1f)
            {
                data.Pkmn.Pos.Sprite.PixelateAmt = 0f;
                _tasks.RemoveAndDispose(task);
                ShowPacketMessageThenResumeBattleThread(data.Packet);
                return;
            }

            data.Pkmn.Pos.Sprite.PixelateAmt = 1f - data.Progress;
        }

        #endregion
    }
}
