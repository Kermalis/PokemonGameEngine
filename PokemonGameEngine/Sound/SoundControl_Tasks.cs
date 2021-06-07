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

            public TaskData_Fade(SoundChannel handle, int milliseconds, float from, float to)
            {
                Handle = handle;
                handle.BeginFade(milliseconds, from, to);
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
                data.Handle.BeginFade(milliseconds, from, to); // Update the fade
            }
            else if (s != _overworldBGM.Song) // Don't change if it's the current song
            {
                var data = new TaskData_FadeSongToSong(s, _overworldBGM.Channel, milliseconds, from, to);
                _tasks.Add(Task_FadeOverworldBGMAndStartNewSong, int.MaxValue, data: data);
            }
        }

        private static void CreateFadeBattleBGMToOverworldBGMTask()
        {
            var data = new TaskData_Fade(_battleBGM.Channel, 1_000, 1f, 0f);
            _tasks.Add(Task_FadeBattleBGMToOverworldBGM, int.MaxValue, data: data);
        }

        private static void Task_FadeOverworldBGMAndStartNewSong(BackTask task)
        {
            var data = (TaskData_FadeSongToSong)task.Data;
            if (!data.Handle.IsFading)
            {
                SoundMixer.StopChannel(_overworldBGM.Channel);
                if (data.Song == Song.None)
                {
                    _overworldBGM = null;
                }
                else
                {
                    _overworldBGM = SongToSound(data.Song);
                    _overworldBGM.Channel = new SoundChannel(_overworldBGM.Wav);
                    SoundMixer.AddChannel(_overworldBGM.Channel);
                }
                _tasks.Remove(task);
            }
        }

        private static void Task_FadeBattleBGMToOverworldBGM(BackTask task)
        {
            var data = (TaskData_Fade)task.Data;
            if (!data.Handle.IsFading)
            {
                SoundMixer.StopChannel(_battleBGM.Channel);
                _battleBGM = null;
                if (_overworldBGM is not null)
                {
                    // Create overworld bgm fade
                    _overworldBGM.Channel.BeginFade(1_000, 0f, 1f);
                    _overworldBGM.Channel.IsPaused = false;
                }
                _tasks.Remove(task);
            }
        }
    }
}
