namespace GB.Core.Serial
{
    public class NullSerialEndpoint : ISerialEndpoint
    {
        public int Transfer(int outgoing)
        {
            return 0;
        }
    }
}
