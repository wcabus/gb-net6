namespace GB.Core.Serial
{
    public interface ISerialEndpoint
    {
        int Transfer(int outgoing);
    }
}
