namespace GB.Core.Sound
{
    public class NullSoundOutput : ISoundOutput
    {
        private const int BufferSize = 1024;
        public const int SampleRate = 22050;

        private readonly byte[] _buffer = new byte[BufferSize];
        private int _i = 0;
        private int _tick;
        private readonly int _divider;

        public NullSoundOutput()
        {
            _divider = Gameboy.TicksPerSec / SampleRate;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Play(int left, int right)
        {
            if (_tick++ != 0)
            {
                _tick %= _divider;
                return;
            }

            left = (int)(left * 0.25);
            right = (int)(right * 0.25);

            left = left < 0 ? 0 : (left > 255 ? 255 : left);
            right = right < 0 ? 0 : (right > 255 ? 255 : right);

            _buffer[_i++] = (byte)left;
            _buffer[_i++] = (byte)right;
            if (_i > BufferSize / 2)
            {
                // _engine?.PlaySound(_buffer, 0, _i);
                _i = 0;
            }

            // Task.Delay(1).GetAwaiter().GetResult();

            // wait until audio is done playing this data
            //while (_engine?.GetQueuedAudioLength() > BufferSize)
            //{
            //    Thread.Sleep(0);
            //}
        }
    }
}
