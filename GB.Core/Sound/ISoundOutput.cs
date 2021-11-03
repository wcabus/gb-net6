namespace GB.Core.Sound
{
    public interface ISoundOutput
    {
        void Start();
        void Stop();
        void Play(int left, int right);
    }
}
