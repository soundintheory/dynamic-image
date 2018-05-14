using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SWMColor = System.Windows.Media.Color;

namespace SoundInTheory.DynamicImage.Util
{
	public class FastBitmap : IDisposable
	{
		#region Variables

		private const int BitsPerPixel = 4;

		private readonly Stream _stream;
		private WriteableBitmap _bitmap;
		private int _width, _height;
		private int _strideWidth;
		private IntPtr _startingPosition;
		private bool _locked;

		#endregion

		#region Properties / Indexer

		public WriteableBitmap InnerBitmap
		{
			get { return _bitmap; }
			set
			{
				_bitmap = value;

				_width = value.PixelWidth;
				_height = value.PixelHeight;
			}
		}

		public int Width
		{
			get { return _width; }
		}

		public int Height
		{
			get { return _height; }
		}

		unsafe public SWMColor this[int x, int y]
		{
			get
			{
#if DEBUG
				if (!_locked)
					throw new InvalidOperationException("Must call Lock() before getting pixel values");

				if (x < 0 || y < 0 || x >= Width || y >= Height)
					throw new ArgumentOutOfRangeException();
#endif

				byte* b = (byte*) _startingPosition + (y * _strideWidth) + (x * BitsPerPixel);
				return SWMColor.FromArgb(*(b + 3), *(b + 2), *(b + 1), *b);
			}

			set
			{
#if DEBUG
				if (!_locked)
					throw new InvalidOperationException("Must call Lock() before setting pixel values");

				if (x < 0 || y < 0 || x >= _width || y >= _height)
					throw new ArgumentOutOfRangeException();
#endif

				byte* b = (byte*) _startingPosition + (y * _strideWidth) + (x * BitsPerPixel);
				*b = value.B;
				*(b + 1) = value.G;
				*(b + 2) = value.R;
				*(b + 3) = value.A;
			}
		}

		#endregion

		#region Constructors / Destructor

		public FastBitmap(int width, int height)
		{
			InnerBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
		}

		public FastBitmap(string filename, UriKind uriKind = UriKind.Absolute)
		{
            // Use BitmapCacheOption.OnLoad to avoid file locks (thanks Mikhail-Fiadosenka).
            // http://social.msdn.microsoft.com/forums/en-US/wpf/thread/3738345b-a6cc-421d-a98f-d907292d6e35/
            var image = new BitmapImage();
		    image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(filename, uriKind);

            try
            {
                image.EndInit();
            }
            catch (ArgumentException)
            {
                // This exception may mean that the color profile on the image is corrupted - try again and ignore it
                image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(filename, uriKind);
                image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                image.EndInit();
            }

			InnerBitmap = new WriteableBitmap(ConvertFormat(image));
		}

		public FastBitmap(byte[] bytes)
		{
			_stream = new MemoryStream(bytes);
			lock (_stream)
			{
				if (!_stream.CanRead || _stream.Length == 0)
					return;

				try
				{
					InnerBitmap = new WriteableBitmap(ConvertFormat(BitmapDecoder.Create(_stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None).Frames[0]));
				}
				catch (ArgumentException ex)
				{
					throw new DynamicImageException("Specified byte array does not contain an image", ex);
				}
			}
		}

		public FastBitmap(BitmapSource bitmap)
		{
			InnerBitmap = new WriteableBitmap(ConvertFormat(bitmap));
		}

		~FastBitmap()
		{
			Dispose(false);
		}

		#endregion

		private static BitmapSource ConvertFormat(BitmapSource source)
		{
			// If bitmap is anything other than Pbgra32, convert it to Pbgra32 so that we can deal with all images consistently.
			// This does mean that if an image starts off as as 16-bit, we'll be limiting the output to 8-bit, but that's okay
			// for web images.
			if (source != null)
				if (source.Format != PixelFormats.Pbgra32)
					source = new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);
			return source;
		}

		public void Dispose()
		{
			Dispose(true);
			if (_stream != null)
				_stream.Close();
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			Unlock();
		}

		#region Methods

		public void Lock()
		{
			_bitmap.Lock();
			_startingPosition = _bitmap.BackBuffer;
			_strideWidth = _bitmap.BackBufferStride;
			_locked = true;
		}

		public void Unlock()
		{
			if (_locked)
			{
				_bitmap.Unlock();
				_locked = false;
			}
		}

		/// <summary>
		/// Only used for testing.
		/// </summary>
		/// <param name="filename"></param>
		public void Save(string filename)
		{
			using (FileStream stream = File.OpenWrite(filename))
			{
				BitmapEncoder encoder = new BmpBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(_bitmap));
				encoder.Save(stream);
			}
		}

		#endregion
	}
}