namespace GB.Core.Serial
{
    public interface ISerialEndpoint
    {
        bool ExternalClockPulsed();
        int Transfer(int outgoing);
    }
}
