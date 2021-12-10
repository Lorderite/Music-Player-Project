using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Music_Player_Project
{
    class MusicPlayer
    {
        //========================================== Variables and Declerations =====================================================//

        //Audio out
        public WaveOutEvent outputDevice;
        //Audio file Reader
        public AudioFileReader audioFile;
        //Playlist
        public readonly LinkedList<Song> playList = new LinkedList<Song>();
        private LinkedListNode<Song> songNode;
        //Status Variables
        private bool isPlaying = false;
        private bool isPaused = false;
        public bool autoplay = true;
        public bool loopPlaylist = false;

        //getters for status
        public bool IsPlaying { get => isPlaying; private set => isPlaying = value; }
        public bool IsPaused { get => isPaused; private set => isPaused = value; }

        //========================================== Play, Pause, Stop And Resume =====================================================//

        //Play song
        public async Task Play(Song song)
        {
            //Handle stopping of song
            if (outputDevice != null && audioFile != null)
            {
                await Task.Run(() => outputDevice.PlaybackStopped -= OnPlaybackStoppedAsync);
                await Task.Run(() => outputDevice.Stop());
                await Task.Run(() => outputDevice.PlaybackStopped += OnPlaybackStoppedAsync);
            }

            //get song path
            string songPath = song.SongPath;

            //Check output device
            if (outputDevice == null)
            {
                try
                {
                    outputDevice = new WaveOutEvent();
                    outputDevice.PlaybackStopped += OnPlaybackStoppedAsync;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was an error with the audio output device: " + ex, "Error");
                }
            }

            //Check and/or assign song file path
            audioFile = null;
            if (audioFile == null)
            {
                audioFile = new AudioFileReader(songPath);
                outputDevice.Init(audioFile);
                await Task.Run(() => outputDevice.Play());
                isPaused = false;
                isPlaying = true;
            }
        }

        //Play song with add to playlist option
        public async Task Play(Song song, bool addToplaylist)
        {
            //Handle stopping of song
            if (outputDevice != null && audioFile != null)
            {
                await Task.Run(() => outputDevice.PlaybackStopped -= OnPlaybackStoppedAsync);
                await Task.Run(() => outputDevice.Stop());
                await Task.Run(() => outputDevice.PlaybackStopped += OnPlaybackStoppedAsync);
            }

            //get song path
            string songPath = song.SongPath;

            if (addToplaylist)
            {
                playList.AddFirst(song);
                songNode = playList.First;
            }

            //Check output device
            if (outputDevice == null)
            {
                try
                {
                    outputDevice = new WaveOutEvent();
                    outputDevice.PlaybackStopped += OnPlaybackStoppedAsync;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was an error with the audio output device: " + ex, "Error");
                }
            }

            //Check and/or assign song file path
            audioFile = null;
            if (audioFile == null)
            {
                audioFile = new AudioFileReader(songPath);
                outputDevice.Init(audioFile);
                await Task.Run(() => outputDevice.Play());
                isPaused = false;
                isPlaying = true;
            }
        }

        //Song Pausing
        public async Task Pause()
        {
            if (isPaused == false && isPlaying == true)
            {
                await Task.Run(() => outputDevice.Pause());
                isPaused = true;
            }
            else
            {
                throw new Exception("Cannot pause if player is already paused or not playing");
            }
        }

        //Song Resume
        public async Task Resume()
        {
            if (isPaused == true && isPlaying == true)
            {
                await Task.Run(() => outputDevice.Play());
                isPaused = false;
            }
            else
            {
                throw new Exception("Cannot Resume if player is not paused or not playing");
            }
        }

        //Song Stop
        public async Task Stop()
        {
            if (isPlaying == true)
            {
                //Handle stopping of song
                if (outputDevice != null && audioFile != null)
                {
                    await Task.Run(() => outputDevice.PlaybackStopped -= OnPlaybackStoppedAsync);
                    await Task.Run(() => outputDevice.Stop());
                    await Task.Run(() => outputDevice.PlaybackStopped += OnPlaybackStoppedAsync);
                }
            }
        }

        //========================================== First, Previous, Next And Last =====================================================//

        //First song in playlist
        public async Task PlayFirst()
        {
            songNode = playList.First;
            Song temp = songNode.Value;
            await Play(temp);
        }

        //Previous song
        public async Task PlayPrevious()
        {
            if (songNode.Previous != null)
            {
                songNode = songNode.Previous;
                Song temp = songNode.Value;
                await Play(temp);
            }
        }

        //Next Song
        public async Task PlayNext()
        {
            await Play(GetNextSong());
        }

        //Last Song
        public async Task PlayLast()
        {
            if (playList.Count >= 0)
            {
                songNode = playList.Last;
                Song temp = songNode.Value;

                await Play(temp);
            }
        }

        //========================================== Utility/Function Methods =====================================================//

        //Raise playback stopped Event
        private void OnPlaybackStoppedAsync(object sender, StoppedEventArgs args)
        {
            OnPlaybackEnded(EventArgs.Empty);            
        }

        protected virtual void OnPlaybackEnded(EventArgs e)
        {
            PlaybackEnded?.Invoke(this, e);
        }

        public event EventHandler PlaybackEnded;

        //Get the song to play next
        public Song GetNextSong()
        {
            Song nextSong;

            if (songNode != null)
            {
                //Get next song in playlist
                if (songNode.Next != null)
                {
                    //Get next song
                    songNode = songNode.Next;
                    //Assign next song
                    nextSong = songNode.Value;
                }
                //Loop playlist if at end
                else if (loopPlaylist)
                {
                    //get first song of playlist
                    songNode = playList.First;
                    //assign as next song
                    nextSong = songNode.Value;
                }
                //Get next natural song
                else
                {
                    //Prepare idx of next song to play
                    int songidxToPlay = (songNode.Value.SongIdx + 1);
                    //Check if idx is valid
                    if (!(songidxToPlay > MainWindow.Library.Count-1))
                    {
                        //Add that song to playlist
                        playList.AddLast(MainWindow.Library[songidxToPlay]);
                        songNode = songNode.Next;
                        nextSong = songNode.Value;
                    }
                    else //Loop back to first natural song
                    {
                        playList.AddLast(MainWindow.Library[0]);
                        songNode = songNode.Next;
                        nextSong = songNode.Value;
                    }
                }

                return nextSong;
            }
            else//Default to first natural song
            {

                playList.AddLast(MainWindow.Library[0]);
                songNode = playList.First;
                nextSong = songNode.Value;
                return nextSong;
            }

        }

        //Add song to playlist
        public async Task AddToPlaylist(Song song)
        {
            await Task.Run(() => playList.AddLast(song));
        }

        public async Task ClearPlaylist()
        {
            await Task.Run(() => playList.Clear());
        }

        //Nowplaying
        public Song NowPlaying()
        {
            return songNode.Value;
        }
    }
}