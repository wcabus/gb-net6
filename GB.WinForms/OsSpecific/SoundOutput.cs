using GB.Core;
using GB.Core.Sound;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace GB.WinForms.OsSpecific
{
    internal class SoundOutput : ISoundOutput
    {
        private const int BufferSize = 1024;
        public const int SampleRate = 22050;

        private readonly byte[] _buffer = new byte[BufferSize];
        private int _i = 0;
        private int _tick;
        private readonly int _divider;
        private AudioPlaybackEngine? _engine;

        public SoundOutput()
        {
            _divider = Gameboy.TicksPerSec / SampleRate;
        }

        public void Start()
        {
            _engine = new AudioPlaybackEngine(SampleRate, 2);
        }

        public void Stop()
        {
            _engine?.Dispose();
            _engine = null;
        }

        public void Play(int left, int right)
        {
            if (_tick++ != 0)
            {
                _tick %= _divider;
                return;
            }

            left = left < 0 ? 0 : (left > 255 ? 255 : left);
            right = right < 0 ? 0 : (right > 255 ? 255 : right);

            _buffer[_i++] = (byte)left;
            _buffer[_i++] = (byte)right;
            if (_i > BufferSize / 2)
            {
                _engine?.PlaySound(_buffer, 0, _i);
                _i = 0;
            }

            // wait until audio is done playing this data
            while (_engine?.GetQueuedAudioLength() > BufferSize)
            {
                Thread.Sleep(0);
            }
        }
    }

    public class AudioPlaybackEngine : IDisposable
    {
        private IWavePlayer _outputDevice;
        private readonly MixingSampleProvider _mixer;
        private readonly BufferedWaveProvider _bufferedWaveProvider;

        public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
        {
            _outputDevice = new WasapiOut();
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
            {
                ReadFully = true
            };

            _bufferedWaveProvider = new BufferedWaveProvider(WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, SoundOutput.SampleRate, 2, SoundOutput.SampleRate, 8, 8))
            {
                ReadFully = true,
                DiscardOnBufferOverflow = true
            };

            AddMixerInput(_bufferedWaveProvider.ToSampleProvider());
            _outputDevice.Init(_mixer);
            _outputDevice.Play();
        }

        public int GetQueuedAudioLength()
        {
            return _bufferedWaveProvider.BufferedBytes;
        }

        public void PlaySound(byte[] buffer, int offset, int count)
        {
            _bufferedWaveProvider.AddSamples(buffer, offset, count);
        }

        public void PlaySound(string fileName)
        {
            var input = new AudioFileReader(fileName);
            AddMixerInput(new AutoDisposeFileReader(input));
        }

        public void PlaySound(CachedSound sound)
        {
            AddMixerInput(new CachedSoundSampleProvider(sound));
        }

        private void AddMixerInput(ISampleProvider input)
        {
            _mixer.AddMixerInput(ConvertToRightChannelCount(input));
        }

        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == _mixer.WaveFormat.Channels)
            {
                return input;
            }
            if (input.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
            {
                return new MonoToStereoSampleProvider(input);
            }
            throw new NotImplementedException("Not yet implemented this channel count conversion");
        }

        public void Dispose()
        {
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
        }
    }

    public class BufferWaveProvider : IWaveProvider
    {
        private readonly byte[] _buffer;
        private readonly int _count;
        private int _offset;

        public BufferWaveProvider(byte[] buffer, int offset, int count)
        {
            WaveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, SoundOutput.SampleRate, 2, SoundOutput.SampleRate, 8, 8);
            _buffer = buffer;
            _offset = offset;
            _count = count;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(byte[] buffer, int offset, int count)
        {
            var availableSamples = _count - _offset;
            if (availableSamples <= 0)
            {
                return 0;
            }

            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(_buffer, _offset, buffer, offset, samplesToCopy);

            _offset += samplesToCopy;
            return samplesToCopy;
        }
    }

    public class AutoDisposeFileReader : ISampleProvider
    {
        private readonly AudioFileReader _reader;
        private bool _isDisposed;
        public AutoDisposeFileReader(AudioFileReader reader)
        {
            this._reader = reader;
            this.WaveFormat = reader.WaveFormat;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (_isDisposed)
                return 0;

            var read = _reader.Read(buffer, offset, count);
            if (read == 0)
            {
                _reader.Dispose();
                _isDisposed = true;
            }
            return read;
        }

        public WaveFormat WaveFormat { get; private set; }
    }
    public class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound _cachedSound;
        private long _position;

        public CachedSoundSampleProvider(CachedSound cachedSound)
        {
            this._cachedSound = cachedSound;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = _cachedSound.AudioData.Length - _position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(_cachedSound.AudioData, _position, buffer, offset, samplesToCopy);
            _position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat { get { return _cachedSound.WaveFormat; } }
    }

    public class CachedSound
    {
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }
        public CachedSound(string audioFileName)
        {
            using (var audioFileReader = new AudioFileReader(audioFileName))
            {
                // TODO: could add resampling in here if required
                WaveFormat = audioFileReader.WaveFormat;
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }
                AudioData = wholeFile.ToArray();
            }
        }
    }
}
