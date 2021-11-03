using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GB.WinForms.OsSpecific
{
    internal class GameboyDisplayFrame : IDisposable
    {
        public static readonly int DisplayWidth = 160;
        public static readonly int DisplayHeight = 144;

        private int[]? _pixels;
        private Image<Rgba32> _imageBuffer = new(DisplayWidth, DisplayHeight);
        private MemoryStream _imageStream = new();
        private bool disposedValue;

        public GameboyDisplayFrame()
        {

        }
            
        public void SetPixels(int[] pixels) => _pixels = pixels;

        public IEnumerable<int[]> Rows()
        {
            if (_pixels == null)
            {
                yield break;
            }

            var offset = 0;
            for (var row = 0; row < DisplayHeight; row++)
            {
                var thisRow = new int[DisplayWidth];
                Array.Copy(_pixels, offset, thisRow, 0, 160);
                yield return thisRow;
                offset += 160;
            }
        }

        public Stream ToBitmap()
        {
            var x = 0;
            var y = 0;

            if (_pixels != null)
            {
                foreach (var pixel in _pixels)
                {
                    if (x == DisplayWidth)
                    {
                        x = 0;
                        y++;
                    }

                    var (r, g, b) = pixel.ToRgb();
                    _imageBuffer[x, y] = new Rgba32((byte)r, (byte)g, (byte)b, 255);

                    x++;
                }
            }

            _imageStream.Seek(0, SeekOrigin.Begin);
            _imageBuffer.SaveAsBmp(_imageStream);
            return _imageStream;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _imageBuffer.Dispose();
                    _imageStream.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal static class GameboyDisplayFrameHelperExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int, int) ToRgb(this int pixel)
        {
            var b = pixel & 255;
            var g = (pixel >> 8) & 255;
            var r = (pixel >> 16) & 255;
            return (r, g, b);
        }
    }
}
