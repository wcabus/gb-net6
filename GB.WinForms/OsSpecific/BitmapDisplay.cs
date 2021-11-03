using GB.Core.Graphics;

namespace GB.WinForms.OsSpecific
{
    internal class BitmapDisplay : IDisplay
    {
        public static readonly int DisplayWidth = 160;
        public static readonly int DisplayHeight = 144;

        public static readonly int[] Colors = { 0xe6f8da, 0x99c886, 0x437969, 0x051f2a };

        private readonly GameboyDisplayFrame _frame = new();
        private readonly int[] _rgb;
        private int _doRefresh;
        private int _i;

        private long _ticks;

        public event EventHandler OnFrameProduced = (_, _) => { };

        public BitmapDisplay()
        {
            _rgb = new int[DisplayWidth * DisplayHeight];
        }

        public bool Enabled { get; set; }

        public void PutDmgPixel(int color)
        {
            _rgb[_i++] = Colors[color];
            _i %= _rgb.Length;
        }

        public void PutColorPixel(int gbcRgb)
        {
            _rgb[_i++] = TranslateGbcRgb(gbcRgb);
        }

        public static int TranslateGbcRgb(int gbcRgb)
        {
            var r = (gbcRgb >> 0) & 0x1f;
            var g = (gbcRgb >> 5) & 0x1f;
            var b = (gbcRgb >> 10) & 0x1f;
            var result = (r * 8) << 16;
            result |= (g * 8) << 8;
            result |= (b * 8) << 0;
            return result;
        }

        public void RequestRefresh() => Interlocked.Exchange(ref _doRefresh, 1);

        public void WaitForRefresh()
        {
            // while (_doRefresh) => fill buffer with pixel data
            while (Interlocked.CompareExchange(ref _doRefresh, 1, 1) == 1)
            {
                continue;
            }
        }

        public void Run(CancellationToken token)
        {
            Interlocked.Exchange(ref _doRefresh, 0);
            _ticks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Enabled = true;

            while (!token.IsCancellationRequested)
            {
                // if (!_doRefresh)
                if (Interlocked.CompareExchange(ref _doRefresh, 0, 0) == 0)
                {
                    continue;
                }

                RefreshScreen();
                Interlocked.Exchange(ref _doRefresh, 0);
            }
        }

        public Stream GetFrame()
        {
            return _frame.ToBitmap();
        }

        private void RefreshScreen()
        {
            _frame.SetPixels(_rgb);

            var ticks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var elapsed = ticks - _ticks;
            if (elapsed > 0)
            {
                var sleep = (int)((1000 / 60.0) - elapsed);
                if (sleep > 0)
                {
                    Thread.Sleep(sleep);
                }
            }

            _ticks = ticks;
            OnFrameProduced?.Invoke(this, EventArgs.Empty);
            _i = 0;
        }
    }
}
