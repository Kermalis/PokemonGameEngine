using Kermalis.PokemonBattleEngine.Packets;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.R3D;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed partial class BattleGUI
    {
        private readonly ConnectedList<BackTask> _tasks = new(BackTask.Sorter);

        private void RunTasks()
        {
            for (BackTask t = _tasks.First; t is not null; t = t.Next)
            {
                t.Action(t);
            }
        }

        #region Misc

        private sealed class TaskData_RenderWhite
        {
            public float Progress;
        }

        private void Task_RenderWhite(BackTask task)
        {
            var data = (TaskData_RenderWhite)task.Data;
            data.Progress += Display.DeltaTime * 2f; // Half a second
            if (data.Progress >= 1f)
            {
                _tasks.Remove(task);
                ActuallyStartFadeIn();
            }
        }

        #endregion

        #region Messages

        private const float AUTO_ADVANCE_SECONDS = 3f;

        private sealed class TaskData_PrintMessage
        {
            public Action OnFinished;
        }

        private void ClearMessage()
        {
            _stringPrinter?.Dispose();
            _stringPrinter = null;
        }
        private void SetMessage_Internal(string str, Action onRead, BackTaskAction a)
        {
            _stringPrinter?.Dispose();
            _stringPrinter = null;
            if (str is not null)
            {
                _stringPrinter = new StringPrinter(_stringWindow, str, Font.Default, FontColors.DefaultWhite_I, new Vec2I(16, 0));
                var data = new TaskData_PrintMessage
                {
                    OnFinished = onRead
                };
                _tasks.Add(new BackTask(a, int.MaxValue, data: data));
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
            if (!_stringPrinter.IsEnded || !_canAdvanceMsg)
            {
                return;
            }

            if (_stringPrinter.IsDone || (_autoAdvanceTime += Display.DeltaTime) >= AUTO_ADVANCE_SECONDS)
            {
                _autoAdvanceTime = 0f;
                var data = (TaskData_PrintMessage)task.Data;
                _tasks.Remove(task);
                data.OnFinished();
            }
        }
        private void Task_ReadOutStaticMessage(BackTask task)
        {
            _stringPrinter.Update();
            if (!_stringPrinter.IsEnded || !_canAdvanceMsg)
            {
                return;
            }

            var data = (TaskData_PrintMessage)task.Data;
            _tasks.Remove(task);
            data.OnFinished();
        }

        #endregion

        #region Camera Motion

        private const float CAM_SPEED_FAST = 0.25f;
        private const float CAM_SPEED_DEFAULT = 0.5f;

        public static readonly PositionRotation DefaultCamPosition = new(new Vector3(7f, 7f, 15f), new Rotation(-22f, 13f, 0f));
        private static readonly PositionRotation _camPosThrowBall = new(new Vector3(16.9f, 7.5f, 30.6f), new Rotation(-32.4f, 8.8f, 0f));
        /// <summary>The position the camera goes to show all pkmn when selecting moves or in certain situations like Earthquake.
        /// Perish Song uses a different position which is the same for every format.
        /// In 1v1 and rotation, this is unused completely since selecting a move does not change the camera and Earthquake goes to the center foe's spot instead.
        /// In 1v1 and rotation, when the back button is pressed, it does snap back to the center ally's spot though</summary>
        private static readonly PositionRotation _camPosViewAll = new(new Vector3(9f, 8f, 22f), new Rotation(-22f, 13f, 0f));

        private sealed class TaskData_MoveCamera
        {
            public const int Tag = 0x2192A9;

            public Action OnFinished;
            public PositionRotationAnimator Animator;

            public TaskData_MoveCamera(Camera c, PositionRotationAnimator.Method m, in PositionRotation to, Action onFinished, float seconds)
            {
                OnFinished = onFinished;
                Animator = new PositionRotationAnimator(m, c.PR, to, seconds);
            }
        }

        private void CreateCameraMotionTask(TaskData_MoveCamera data)
        {
            _tasks.Add(new BackTask(Task_CameraMotion, int.MaxValue, data: data, tag: TaskData_MoveCamera.Tag));
        }
        private void CreateCameraMotionTask(in PositionRotation to, float seconds, Action onFinished = null, PositionRotationAnimator.Method method = PositionRotationAnimator.Method.Linear)
        {
            CreateCameraMotionTask(new TaskData_MoveCamera(Camera, method, to, onFinished, seconds));
        }

        private void Task_CameraMotion(BackTask task)
        {
            var data = (TaskData_MoveCamera)task.Data;
            if (data.Animator.Update(ref Camera.PR))
            {
                _tasks.Remove(task);
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
            public float BlacknessProgress;
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
                data.BlacknessProgress = (data.DelayProgress - 1f) * TaskData_TrainerReveal.SPEED;
                data.DelayProgress = 1f;
            }
            else
            {
                data.BlacknessProgress += Display.DeltaTime * TaskData_TrainerReveal.SPEED;
            }
            if (data.BlacknessProgress >= 1f)
            {
                _trainerSprite.BlacknessAmt = 0f;
                _trainerSprite.Image.IsPaused = false;
                task.Data = null;
                task.Action = Task_TrainerWaitAnim;
                return;
            }
            _trainerSprite.BlacknessAmt = 1f - data.BlacknessProgress;
        }
        private void Task_TrainerWaitAnim(BackTask task)
        {
            if (!_trainerSprite.Image.IsPaused)
            {
                return;
            }

            _canAdvanceMsg = true;
            _tasks.Remove(task);
        }

        private void Task_TrainerGoAway(BackTask task)
        {
            var data = (TaskData_TrainerGoAway)task.Data;
            data.Progress += Display.DeltaTime;
            if (data.Progress >= 1f)
            {
                _trainerSprite.IsVisible = false;
                _tasks.Remove(task); // Don't delete sprite here since we can reuse it for taunts in-battle
                return;
            }
            _trainerSprite.Pos.Z = Utils.Lerp(data.StartZ, data.EndZ, data.Progress);
            _trainerSprite.Opacity = 1f - data.Progress;
        }

        #endregion

        #region Pokemon Sprite

        private sealed class TaskData_WildReveal
        {
            public const float START_AMT = 0.75f;

            public float Progress;
        }
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

        private void Task_WildReveal(BackTask task)
        {
            void Set(float amt)
            {
                foreach (PkmnPosition p in _positions[1])
                {
                    p.Sprite.BlacknessAmt = amt;
                }
            }

            var data = (TaskData_WildReveal)task.Data;
            data.Progress += Display.DeltaTime;
            if (data.Progress >= 1f)
            {
                _tasks.Remove(task);
                Set(0f);
                return;
            }

            Set(Utils.Lerp(TaskData_WildReveal.START_AMT, 0f, data.Progress));
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
                _tasks.Remove(task);
                data.Pkmn.Pos.Sprite.PixelateAmt = 0f;
                _hudInvisible = false;
                ShowPacketMessageThenResumeBattleThread(data.Packet);
                return;
            }

            data.Pkmn.Pos.Sprite.PixelateAmt = 1f - data.Progress;
        }

        #endregion
    }
}
