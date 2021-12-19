using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal static partial class SoundControl
    {
        #region Task Data

        private class TaskData_Fade
        {
            public readonly SoundChannel Channel;

            public TaskData_Fade(SoundChannel channel, float seconds, float from, float to)
            {
                Channel = channel;
                channel.BeginFade(seconds, from, to);
            }
        }
        private sealed class TaskData_FadeSongToSong : TaskData_Fade
        {
            public Song Song;

            public TaskData_FadeSongToSong(Song s, SoundChannel channel, float seconds, float from, float to)
                : base(channel, seconds, from, to)
            {
                Song = s;
            }
        }

        #endregion

        private static void CreateOrUpdateFadeOverworldBGMAndStartNewSongTask(Song s, float seconds, float from, float to)
        {
            if (_tasks.TryGetTask(Task_FadeOverworldBGMAndStartNewSong, out BackTask existing))
            {
                var data = (TaskData_FadeSongToSong)existing.Data;
                data.Song = s;
                data.Channel.BeginFade(seconds, from, to); // Update the fade
            }
            else if (s != _overworldBGM.Song) // Don't change if it's the current song
            {
                var data = new TaskData_FadeSongToSong(s, _overworldBGM.Channel, seconds, from, to);
                _tasks.Add(Task_FadeOverworldBGMAndStartNewSong, int.MaxValue, data: data);
            }
        }

        private static void CreateFadeBattleBGMToOverworldBGMTask()
        {
            var data = new TaskData_Fade(_battleBGM.Channel, 1f, 1f, 0f);
            _tasks.Add(Task_FadeBattleBGMToOverworldBGM, int.MaxValue, data: data);
        }

        private static void Task_FadeOverworldBGMAndStartNewSong(BackTask task)
        {
            var data = (TaskData_FadeSongToSong)task.Data;
            if (data.Channel.IsFading)
            {
                return;
            }

            _tasks.RemoveAndDispose(task);
            SoundMixer.StopChannel(_overworldBGM.Channel);
            if (data.Song == Song.None)
            {
                _overworldBGM = null;
            }
            else
            {
                _overworldBGM = LoadSongSound(data.Song);
                _overworldBGM.Channel = new SoundChannel(_overworldBGM.Data);
                SoundMixer.AddChannel(_overworldBGM.Channel);
            }
        }

        private static void Task_FadeBattleBGMToOverworldBGM(BackTask task)
        {
            var data = (TaskData_Fade)task.Data;
            if (data.Channel.IsFading)
            {
                return;
            }

            _tasks.RemoveAndDispose(task);
            SoundMixer.StopChannel(_battleBGM.Channel);
            _battleBGM = null;
            if (_overworldBGM is not null)
            {
                // Create overworld bgm fade
                _overworldBGM.Channel.BeginFade(1f, 0f, 1f);
                _overworldBGM.Channel.IsPaused = false;
            }
        }
    }
}
