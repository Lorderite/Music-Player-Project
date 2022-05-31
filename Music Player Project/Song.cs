using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Music_Player_Project
{
	public class Song : IComparable
	{
		//Variables
		protected readonly string songName;
		private readonly string songPath;
		private readonly int songIdx;
		private readonly BitmapImage imageData;

		//Gets
		public string SongName { get => songName; }
		public string SongPath { get => songPath; }
		public BitmapImage ImageData { get => imageData; }
		public int SongIdx { get => songIdx; }

		//Constructer
		public Song(string songName, string songPath, int songIdx)
		{
			this.songName = songName;
			this.songPath = songPath;
			this.songIdx = songIdx;
			imageData = GetCoverImage(songPath);
		}

		//Comparer
		public int CompareTo(Object obj)
		{
			var otherSong = (Song)obj;

			if (otherSong == null) return 1;//Handle invalid object 

			if (otherSong.songName != null)
			{
				return songName.CompareTo(otherSong.songName);
			}
			else
			{
				throw new ArgumentException("Error in comparison, object must be invalid");
			}
		}

		//Get File's cover image
		public BitmapImage GetCoverImage(string songPath)
		{
			BitmapImage coverImage;
			TagLib.File file = TagLib.File.Create(songPath);
			var firstPic = file.Tag.Pictures.FirstOrDefault();
			if (firstPic != null)
			{
				byte[] pictureData = firstPic.Data.Data;
				coverImage = ToImage(pictureData);
			}
			else
			{
				//Set default cover Image
				coverImage = new BitmapImage(new Uri(@"uiresources\default.png", UriKind.Relative));
			}

			file.Dispose();
			return coverImage;
		}

		public void SetFileCoverImage(BitmapImage image)
		{
			try
			{
				using (TagLib.File file = TagLib.File.Create(SongPath))
				{
					TagLib.Id3v2.AttachedPictureFrame cover = new TagLib.Id3v2.AttachedPictureFrame
					{
						Type = TagLib.PictureType.FrontCover,
						Description = "Cover",
						MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
						Data = ToByteArray(image),
						TextEncoding = TagLib.StringType.UTF16


					};
					file.Tag.Pictures = new TagLib.IPicture[] { cover };
					file.Save();
				}
			}
			catch (Exception)
			{
				MessageBox.Show("Cannot edit song that is Playing", "Error");
			}
		}

		//========================================== Conversion Methods =====================================================//

		public BitmapImage ToImage(byte[] array)
		{
			using (var ms = new MemoryStream(array))
			{
				var image = new BitmapImage();
				image.BeginInit();
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.StreamSource = ms;
				image.EndInit();
				return image;
			}
		}

		public byte[] ToByteArray(BitmapImage image)
		{
			byte[] array;
			JpegBitmapEncoder encoder = new JpegBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(image));
			using (MemoryStream memStream = new MemoryStream())
			{
				encoder.Save(memStream);
				array = memStream.ToArray();
			}
			return array;
		}

		public override string ToString()
		{
			string output = SongName;
			return output;
		}
	}
}
