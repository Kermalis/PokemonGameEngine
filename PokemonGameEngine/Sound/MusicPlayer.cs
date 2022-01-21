using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;

namespace Kermalis.PokemonGameEngine.Sound
{
    internal sealed class MusicPlayer
    {
        private const float STANDARD_FADE_OUT_LENGTH = 1f;

        public static MusicPlayer Main { get; } = new MusicPlayer();

        public Song Music;
        private SoundChannel _channel;

        private bool _isFadingFromOtherMusic;
        private BackTask _fadeMusicTask;
        private Song _backupMusic;
        private SoundChannelState _backupSoundState;

        public void QueueMusicIfDifferentThenFadeOutCurrentMusic(Song newMusic)
        {
            if (newMusic == Music)
            {
                return;
            }

            lock (SoundControl.LockObj)
            {
                Music = newMusic; // newMusic can be none
                _backupSoundState = null;
                if (_channel is null)
                {
                    _isFadingFromOtherMusic = false;
                }
                else
                {
                    _isFadingFromOtherMusic = true;
                    CreateOrSetFadeOutMusicTask(Task_FadeToNothing);
                }
            }
        }
        public void FadeToQueuedMusic()
        {
            lock (SoundControl.LockObj)
            {
                _backupSoundState = null;
                if (_channel is null)
                {
                    _isFadingFromOtherMusic = false;
                    if (Music != Song.None)
                    {
                        CreateAndStartMusicChannel(); // Start new music if there is one
                    }
                }
                else
                {
                    _isFadingFromOtherMusic = true;
                    CreateOrSetFadeOutMusicTask(Task_FadeToQueuedMusic);
                }
            }
        }
        public void FadeToNewMusic(Song newMusic)
        {
            if (Music == newMusic)
            {
                return;
            }

            lock (SoundControl.LockObj)
            {
                Music = newMusic;
                _backupSoundState = null;
                if (newMusic == Song.None)
                {
                    if (_channel is null)
                    {
                        _isFadingFromOtherMusic = false;
                    }
                    else
                    {
                        _isFadingFromOtherMusic = true;
                        CreateOrSetFadeOutMusicTask(Task_FadeToNothing); // Fade to nothing
                    }
                }
                else
                {
                    if (_channel is null)
                    {
                        _isFadingFromOtherMusic = false;
                        CreateAndStartMusicChannel(); // Instantly start new music
                    }
                    else
                    {
                        _isFadingFromOtherMusic = true;
                        CreateOrSetFadeOutMusicTask(Task_FadeToQueuedMusic); // Fade to new music
                    }
                }
            }
        }
        public void FadeToBackupMusic()
        {
            lock (SoundControl.LockObj)
            {
                Music = _backupMusic;
                _backupMusic = Song.None;
                if (_channel is null)
                {
                    _isFadingFromOtherMusic = false;
                    RestoreBackupStateAndFadeInMusic();
                }
                else
                {
                    _isFadingFromOtherMusic = true;
                    CreateOrSetFadeOutMusicTask(Task_FadeToBackupState);
                }
            }
        }

        public void BeginNewMusicAndBackupCurrentMusic(Song newMusic)
        {
            lock (SoundControl.LockObj)
            {
                // Create backup state first
                _backupMusic = Music;
                if (_channel is null)
                {
                    _backupSoundState = null;
                }
                else
                {
                    if (_isFadingFromOtherMusic)
                    {
                        // backup state is either null or already set correctly (if this is called again before Task_FadeToBackupState() finishes)
                    }
                    else
                    {
                        _backupSoundState = _channel.GetState(); // If we are on the current music, backup the current state
                    }
                    SoundMixer.StopChannel(_channel);
                }

                // Remove current fade task
                if (_fadeMusicTask is not null)
                {
                    SoundControl.Tasks.Remove(_fadeMusicTask);
                    _fadeMusicTask = null;
                }
                _isFadingFromOtherMusic = false;

                // Instantly begin battle music
                Music = newMusic;
                CreateAndStartMusicChannel();
            }
        }

        private void CreateOrSetFadeOutMusicTask(BackTaskAction action)
        {
            if (_fadeMusicTask is null)
            {
                _channel.BeginFade(STANDARD_FADE_OUT_LENGTH, 1f, 0f); // Create task and fade out current music
                _fadeMusicTask = new BackTask(action, 0);
                SoundControl.Tasks.Add(_fadeMusicTask);
            }
            else
            {
                _fadeMusicTask.Action = action; // We are already fading, so change the action
            }
        }
        private void CreateAndStartMusicChannel()
        {
            _channel = new SoundChannel(SoundControl.GetSongAsset(Music));
            SoundMixer.AddChannel(_channel);
        }
        private void RestoreBackupStateAndFadeInMusic()
        {
            if (_backupSoundState is null)
            {
                if (Music == Song.None)
                {
                    _channel = null;
                }
                else
                {
                    CreateAndStartMusicChannel();
                }
            }
            else
            {
                _channel = new SoundChannel(_backupSoundState);
                _backupSoundState = null;
                SoundMixer.AddChannel(_channel);

                _channel.BeginFade(STANDARD_FADE_OUT_LENGTH, 0f, 1f);
                _fadeMusicTask = new BackTask(Task_FadeFromNothing, 0);
                SoundControl.Tasks.Add(_fadeMusicTask);
            }
        }

        // For the tasks below, "task" is "_fadeMusicTask"
        private void Task_FadeToNothing(BackTask task)
        {
            if (_channel.IsFading)
            {
                return;
            }

            SoundControl.Tasks.Remove(task);
            _fadeMusicTask = null;
            _isFadingFromOtherMusic = false;
            SoundMixer.StopChannel(_channel);

            _channel = null; // Don't touch _curMusic since it could be queued
        }
        private void Task_FadeFromNothing(BackTask task)
        {
            if (_channel.IsFading)
            {
                return;
            }

            SoundControl.Tasks.Remove(task);
            _fadeMusicTask = null;
        }
        private void Task_FadeToQueuedMusic(BackTask task)
        {
            if (_channel.IsFading)
            {
                return;
            }

            SoundControl.Tasks.Remove(task);
            _fadeMusicTask = null;
            _isFadingFromOtherMusic = false;
            SoundMixer.StopChannel(_channel);

            if (Music == Song.None)
            {
                _channel = null;
            }
            else
            {
                CreateAndStartMusicChannel();
            }
        }
        private void Task_FadeToBackupState(BackTask task)
        {
            if (_channel.IsFading)
            {
                return;
            }

            SoundControl.Tasks.Remove(task);
            _fadeMusicTask = null;
            _isFadingFromOtherMusic = false;
            SoundMixer.StopChannel(_channel);

            RestoreBackupStateAndFadeInMusic();
        }
    }
}
