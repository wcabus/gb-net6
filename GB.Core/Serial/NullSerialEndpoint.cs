namespace GB.Core.Serial
{
    public class NullSerialEndpoint : ISerialEndpoint
    {
        public bool ExternalClockPulsed() => false;
        public int Transfer(int outgoing)
        {
            return (outgoing << 1) & 0xFF;
        }
    }
}
