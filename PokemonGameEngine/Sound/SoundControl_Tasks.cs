using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;
using System;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal static partial class SoundControl
    {
        #region Task Data

        private class TaskData_Fade
        {
            public readonly SoundChannel Handle;
            public TimeSpan CurTime;
            public readonly TimeSpan EndTime;
            public readonly float From;
            public readonly float To;

            public TaskData_Fade(SoundChannel handle, int milliseconds, float from, float to)
            {
                handle.EffectVolume = from;
                Handle = handle;
                CurTime = new TimeSpan();
                EndTime = TimeSpan.FromMilliseconds(milliseconds);
                From = from;
                To = to;
            }

            public float Update()
            {
                float vol = SoundMixer.UpdateFade(From, To, EndTime, ref CurTime);
                Handle.EffectVolume = vol;
                return vol;
            }
        }
        private class TaskData_FadeSongToSong : TaskData_Fade
        {
            public Song Song;

            public TaskData_FadeSongToSong(Song s, SoundChannel handle, int milliseconds, float from, float to)
                : base(handle, milliseconds, from, to)
            {
                Song = s;
            }
        }

        #endregion

        private static void CreateOrUpdateFadeOverworldBGMAndStartNewSongTask(Song s, int milliseconds, float from, float to)
        {
            if (_tasks.TryGetTask(Task_FadeOverworldBGMAndStartNewSong, out BackTask existing))
            {
                var data = (TaskData_FadeSongToSong)existing.Data;
                data.Song = s;
            }
            else if (s != _overworldBGM.Song) // Don't change if it's the current song
            {
                var data = new TaskData_FadeSongToSong(s, _overworldBGM.Handle, milliseconds, from, to);
                _tasks.Add(Task_FadeOverworldBGMAndStartNewSong, int.MaxValue, data: data);
            }
        }
        private static void CreateFadeOverworldBGMTask(int milliseconds, float from, float to)
        {
            var data = new TaskData_Fade(_overworldBGM.Handle, milliseconds, from, to);
            _tasks.Add(Task_Fade, int.MaxValue, data: data);
        }

        private static void CreateFadeBattleBGMToOverworldBGMTask()
        {
            var data = new TaskData_Fade(_battleBGM.Handle, 1_000, 1f, 0f);
            _tasks.Add(Task_FadeBattleBGMToOverworldBGM, int.MaxValue, data: data);
        }

        private static void Task_FadeOverworldBGMAndStartNewSong(BackTask task)
        {
            var data = (TaskData_FadeSongToSong)task.Data;
            float vol = data.Update();
            if (vol == data.To)
            {
                SoundMixer.StopSound(_overworldBGM.Handle);
                _overworldBGM.Dispose();
                if (data.Song == Song.None)
                {
                    _overworldBGM = null;
                }
                else
                {
                    _overworldBGM = SongToSound(data.Song);
                    _overworldBGM.Handle = SoundMixer.StartSound(_overworldBGM.Wav);
                }
                _tasks.Remove(task);
                return;
            }
        }

        private static void Task_FadeBattleBGMToOverworldBGM(BackTask task)
        {
            var data = (TaskData_Fade)task.Data;
            float vol = data.Update();
            if (vol == data.To)
            {
                SoundMixer.StopSound(_battleBGM.Handle);
                _battleBGM.Dispose();
                _battleBGM = null;
                if (_overworldBGM is not null)
                {
                    data = new TaskData_Fade(_overworldBGM.Handle, 1_000, 0f, 1f);
                    task.Data = data;
                    task.Action = Task_Fade;
                    _overworldBGM.Handle.IsPaused = false;
                    return;
                }
                _tasks.Remove(task);
                return;
            }
        }

        private static void Task_Fade(BackTask task)
        {
            var data = (TaskData_Fade)task.Data;
            float vol = data.Update();
            if (vol == data.To)
            {
                _tasks.Remove(task);
                return;
            }
        }
    }
}
