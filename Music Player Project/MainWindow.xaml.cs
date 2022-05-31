using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Music_Player_Project
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		//========================================== Variables and Decleration =====================================================//

		//Global Song Library
		public static List<Song> Library = new List<Song>();
		//Global Account
		public Account account;
		//Global Player
		private readonly MusicPlayer musicPlayer = new MusicPlayer();
		private bool isSeeking = false;
		//Program tick Timer
		private Timer timer;
		//Username referance
		public string UserName
		{
			get { return (string)GetValue(UserNameStringProperty); }
			set { SetValue(UserNameStringProperty, value); }
		}
		public static readonly DependencyProperty UserNameStringProperty = DependencyProperty.Register("UserName", typeof(string), typeof(MainWindow), new PropertyMetadata("default value"));

		//Image preperations
		private readonly Image pauseImage = new Image
		{
			Source = new BitmapImage(new Uri(@"uiresources\pause.png", UriKind.Relative))
		};

		private readonly Image playImage = new Image
		{
			Source = new BitmapImage(new Uri(@"uiresources\play.png", UriKind.Relative))
		};

		//========================================== Startup of Window =====================================================//
		public MainWindow(Account account)
		{
			//Set DataContext
			DataContext = this;

			InitializeComponent();
			RefreshSongsList();

			//Assign global Account
			this.account = account;
			UserName = account.UserName;

			//Setup songEnding Event
			musicPlayer.PlaybackEnded += MusicPlayer_PlaybackEnded;

			//Disable seekbar until seekbar is set
			seekbar.IsEnabled = false;

			//Hush Hush
			if (account.UserName.CompareTo("Dragon Loli") == 0)
			{
				ImageBrush myBrush = new ImageBrush();
				Image image = new Image
				{
					Source = new BitmapImage(new Uri(@"secret\838749.jpg", UriKind.Relative))
				};
				myBrush.ImageSource = image.Source;

				menu.Background = myBrush;
				LibraryListView.Foreground = new SolidColorBrush(Colors.Black);
			}
		}


		//========================================== Event Handlers (Button Clicks) =====================================================//

		//Play previous Song in playlist
		private async void BtnPrev_Click(object sender, RoutedEventArgs e)
		{
			btnPrev.IsEnabled = false;
			var temp = musicPlayer.NowPlaying().SongName;
			await musicPlayer.PlayPrevious();
			btnPlay.Content = pauseImage;
			if (temp.CompareTo(musicPlayer.NowPlaying().SongName) != 0)
				lstUpcoming.Items.Insert(0, temp);
			SetSeekbar();
			btnPrev.IsEnabled = true;
		}

		//Goes to first song in playlist
		private async void BtnFirst_Click(object sender, RoutedEventArgs e)
		{
			btnFirst.IsEnabled = false;
			await musicPlayer.PlayFirst();
			lstUpcoming.Items.Clear();
			foreach (Song song in musicPlayer.playList)
			{
				lstUpcoming.Items.Add(song.SongName);
			}
			if (lstUpcoming.Items.Count > 0)
				lstUpcoming.Items.RemoveAt(0);
			btnPlay.Content = pauseImage;
			SetSeekbar();
			btnFirst.IsEnabled = true;
		}

		//Play click handler
		private async void BtnPlay_Click(object sender, RoutedEventArgs e)
		{
			//If a song is playing and it is not paused, pause it
			if (musicPlayer.IsPlaying && !musicPlayer.IsPaused)
			{
				await musicPlayer.Pause();
				btnPlay.Content = playImage;
			}
			//If a song is playing and it is paused, resume song
			else if (musicPlayer.IsPlaying && musicPlayer.IsPaused)
			{
				await musicPlayer.Resume();
				btnPlay.Content = pauseImage;
			}
			//No song must playing, so start playing first natural song
			else
			{
				await musicPlayer.Play(Library[0], true);
				SetSeekbar();
				btnPlay.Content = pauseImage;
			}
		}

		//Skip to next song
		private async void BtnNext_Click(object sender, RoutedEventArgs e)
		{
			btnNext.IsEnabled = false;
			try
			{
				await musicPlayer.PlayNext();
			}
			catch (Exception ex)
			{
				if (ex is NullReferenceException || ex is ArgumentOutOfRangeException)
				{
					await musicPlayer.Play(musicPlayer.GetNextSong(), true);
					return;
				}
				throw;
			}
			if (lstUpcoming.Items.Count > 0)
				lstUpcoming.Items.RemoveAt(0);
			btnPlay.Content = pauseImage;
			SetSeekbar();
			btnNext.IsEnabled = true;
		}
		//Go to last song
		private async void BtnLast_Click(object sender, RoutedEventArgs e)
		{
			btnLast.IsEnabled = false;
			await musicPlayer.PlayLast();
			lstUpcoming.Items.Clear();
			btnPlay.Content = pauseImage;
			SetSeekbar();
			btnLast.IsEnabled = true;
		}

		//========================================================================//

		//Handle importing songs
		private void BtnImport_Click(object sender, RoutedEventArgs e)
		{

			OpenFileDialog fileDialog = new OpenFileDialog()
			{
				FileName = "Select Audio File",
				Filter = "Audio Files (*.mp3;*.m4a;*.wav)|*.mp3;*.m4a;*.wav|All files (*.*)|*.*",
				Title = "Import Audio File",
				Multiselect = true
			};

			Nullable<bool> result = fileDialog.ShowDialog();

			if (result == true)
			{
				try
				{
					foreach (string Targetfile in fileDialog.FileNames)
					{
						string fileName = System.IO.Path.GetFileName(Targetfile);
						string destination = System.IO.Path.Combine(@"library", fileName);
						File.Copy(Targetfile, destination);
					}
					MessageBox.Show("File copied!", "Success");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error.\n\nError message: {ex.Message}\n\n" +
					$"Details:\n\n{ex.StackTrace}");
				}
			}
			RefreshSongsList();
		}

		//Forced play double click
		private async void SongStackPanel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var temp = (Song)LibraryListView.SelectedItem;
			await musicPlayer.Stop();
			await musicPlayer.ClearPlaylist();
			lstUpcoming.Items.Clear();
			await musicPlayer.Play(temp, true);
			btnPlay.Content = pauseImage;
			SetSeekbar();
		}



		//Clear playlist
		private async void BtnClearPlaylist_Click(object sender, RoutedEventArgs e)
		{
			await musicPlayer.ClearPlaylist();
			lstUpcoming.Items.Clear();
		}

		//========================================== Event Handlers (Menu Items) =====================================================//

		//Forced right click
		private async void MenuItemPlaySong_Click(object sender, RoutedEventArgs e)
		{
			var temp = (Song)LibraryListView.SelectedItem;
			await musicPlayer.Stop();
			await musicPlayer.ClearPlaylist();
			lstUpcoming.Items.Clear();
			await musicPlayer.Play(temp, true);
			btnPlay.Content = pauseImage;
			SetSeekbar();
		}

		//Right click add to playlist
		private async void MenuItemAddToPlaylist_Click(object sender, RoutedEventArgs e)
		{
			var temp = (Song)LibraryListView.SelectedItem;
			lstUpcoming.Items.Add(temp.SongName);
			await musicPlayer.AddToPlaylist(temp);

		}

		//Set album art on song
		private void MenuItemSetAlbumArt_Click(object sender, RoutedEventArgs e)
		{
			int idx = LibraryListView.SelectedIndex;

			OpenFileDialog fileDialog = new OpenFileDialog()
			{
				FileName = "Select Image File",
				Filter = "Image Files (*.png;*.jpg;*.wav)|*.png;*.jpg|All files (*.*)|*.*",
				Title = "Change Audio File Cover Art"
			};

			Nullable<bool> result = fileDialog.ShowDialog();

			if (result == true)
			{
				string path = fileDialog.FileName;
				BitmapImage coverImage = new BitmapImage(new Uri(path, UriKind.Relative));
				Library[idx].SetFileCoverImage(coverImage);
			}

			RefreshSongsList();
		}

		//Open the library folder
		private void MenuItemOpenFileLocation_Click(object sender, RoutedEventArgs e)
		{
			int idx = LibraryListView.SelectedIndex;
			string path = Library[idx].SongPath;

			path = Path.GetDirectoryName(path);

			Process.Start("explorer.exe", path);
		}


		//Delete selected Song
		private void MenuItemDeleteSong_Click(object sender, RoutedEventArgs e)
		{
			int idx = LibraryListView.SelectedIndex;
			string path = Library[idx].SongPath;

			File.Delete(path);
			RefreshSongsList();
		}

		//========================================== Other Events =====================================================//
		//Volume control (Seems a little dodgy?)
		private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (musicPlayer.outputDevice != null)
			{
				musicPlayer.outputDevice.Volume = ((int)SliderVolume.Value / 100f);
			}
		}

		//Start of seeking (When held down)
		private async void Seekbar_MouseDown(object sender, MouseButtonEventArgs e)
		{
			await Task.Run(() => timer.Elapsed -= OnTimedEvent);
			isSeeking = true;
		}

		//Complete seek (Upon Release)
		private void Seekbar_MouseUp(object sender, MouseButtonEventArgs e)
		{
			timer.Elapsed += OnTimedEvent;
			isSeeking = false;
			musicPlayer.audioFile.Position = ((long)(seekbar.Value * musicPlayer.audioFile.WaveFormat.AverageBytesPerSecond));
		}
		private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			//If text has content, search
			if (!String.IsNullOrEmpty(txtSearch.Text))
			{
				List<Song> results = (BinarySearchSongs(txtSearch.Text, Library));
				LibraryListView.ItemsSource = results;
			}
			//If text is empty reset view
			else
			{
				RefreshSongsList();
			}
		}

		private void CbLoopPlaylist_CheckedChanged(object sender, RoutedEventArgs e)
		{
			musicPlayer.loopPlaylist = (bool)cbLoopPlaylist.IsChecked;
		}


		//========================================== Utility/Function Methods =====================================================//

		//Refresh all songs
		private void RefreshSongsList()
		{
			DirectoryInfo dir = new DirectoryInfo(@"library");
			FileInfo[] FilePaths = dir.GetFiles();
			Library.Clear();
			foreach (FileInfo file in FilePaths)
			{
				string filepath = file.FullName;

				string songName = System.IO.Path.GetFileNameWithoutExtension(filepath);
				Song song = new Song(songName, filepath, Library.Count);
				Library.Add(song);
			}

			//Sort
			Library = MergeSort(Library.ToArray()).ToList<Song>();

			LibraryListView.ItemsSource = null;

			LibraryListView.ItemsSource = Library;
		}

		//Autoplay feature
		private async void MusicPlayer_PlaybackEnded(object sender, EventArgs e)
		{
			if (musicPlayer.autoplay == true)
			{
				await musicPlayer.Play(musicPlayer.GetNextSong());
				if (lstUpcoming.Items.Count > 0)
					lstUpcoming.Items.RemoveAt(0);
				btnPlay.Content = pauseImage;
				SetSeekbar();
			}
		}

		//Prepare seekbar for next song
		public void SetSeekbar()
		{
			seekbar.IsEnabled = true;
			seekbar.Maximum = musicPlayer.audioFile.TotalTime.TotalSeconds;
			string content = TimeSpan.FromSeconds(musicPlayer.audioFile.TotalTime.TotalSeconds).ToString(@"mm\:ss");
			lblSongTimeTotal.Content = content;
			timer = new Timer(100);
			timer.Elapsed += OnTimedEvent;
			timer.AutoReset = true;
			timer.Enabled = true;

			Song temp = musicPlayer.NowPlaying();

			NowPlayingImage.Source = temp.ImageData;
			NowPlayingText.Text = temp.SongName;
		}

		//Binary search for all songs 
		private List<Song> BinarySearchSongs(string target, List<Song> dataIn)
		{
			List<Song> result = new List<Song>();
			List<Song> data = new List<Song>(dataIn);
			bool matches = true;
			bool found = false;

			Regex reg = new Regex(@"^" + target + @".*", RegexOptions.IgnoreCase);

			while (matches)
			{
				int min = 0;
				int max = data.Count - 1;
				while (min <= max && !found)
				{
					int mid = (min + max) / 2;
					if (reg.IsMatch(data[mid].SongName))
					{
						result.Add(data[mid]);
						data.RemoveAt(mid);
						found = true;
					}
					else if (target.CompareTo(data[mid].SongName) < 0)
					{
						max = mid - 1;
					}
					else
					{
						min = mid + 1;
					}
				}
				if (!found)
				{
					matches = false;
				}
				found = false;
			}
			return result;
		}

		//Merge sort for songs
		private Song[] MergeSort(Song[] songData)
		{
			//Prepare variables
			Song[] left;
			Song[] right;
			Song[] result;

			//Prevent stack overflow
			if (songData.Length <= 1)
				return songData;

			//Get middle Variable
			int mid = songData.Length / 2;

			//Prepare left path
			left = new Song[mid];

			//Prepare Right path
			if (songData.Length % 2 == 0)//If total is even
				right = new Song[mid];
			else
				right = new Song[mid + 1];//If total is odd

			//Populate left array
			for (int idx = 0; idx < mid; idx++)
			{
				left[idx] = songData[idx];
			}

			//Populate right array
			int rightidx = 0;

			for (int idx = mid; idx < songData.Length; idx++)
			{
				right[rightidx] = songData[idx];
				rightidx++;
			}

			//Sort Arrays
			left = MergeSort(left);
			right = MergeSort(right);

			//Merge sorted Arrays
			result = Merge(left, right);
			return result;
		}
		private Song[] Merge(Song[] left, Song[] right)
		{
			//Prepare Variables
			int totalLength = left.Length + right.Length;

			Song[] result = new Song[totalLength];

			int idxLeft = 0, idxRight = 0, idxResult = 0;

			//Iterate through elements
			while (idxLeft < left.Length || idxRight < right.Length)
			{
				//if both arrays have elements  
				if (idxLeft < left.Length && idxRight < right.Length)
				{
					//Left Side
					if (left[idxLeft].CompareTo(right[idxRight]) <= 0)
					{
						result[idxResult] = left[idxLeft];
						idxLeft++;
						idxResult++;
					}
					//Right Side
					else
					{
						result[idxResult] = right[idxRight];
						idxRight++;
						idxResult++;
					}
				}
				else if (idxLeft < left.Length)
				{
					result[idxResult] = left[idxLeft];
					idxLeft++;
					idxResult++;
				}
				//if only the right array still has elements, add all its items to the results array
				else if (idxRight < right.Length)
				{
					result[idxResult] = right[idxRight];
					idxRight++;
					idxResult++;
				}
			}

			return result;
		}

		//========================================== Timer Events =====================================================//

		//Event to update seekbar
		private void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			try
			{
				this.Dispatcher.Invoke(() =>
				{
					if (!isSeeking)
					{
						seekbar.Value = musicPlayer.audioFile.CurrentTime.TotalSeconds;
					}
					string content = TimeSpan.FromSeconds(musicPlayer.audioFile.CurrentTime.TotalSeconds).ToString(@"mm\:ss");
					lblSongTime.Content = content;
				});
			}
			catch (Exception)
			{
				//Nothing needed
			}
		}
	}
}