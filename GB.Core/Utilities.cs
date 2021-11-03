namespace GB.Core
{
    internal static class Utilities
    {
        public static bool GetBit(this int byteValue, int position) => (byteValue & (1 << position)) != 0;

        public static int SetBit(this int byteValue, int position, bool value) => value ? SetBit(byteValue, position) : ClearBit(byteValue, position);
        public static int SetBit(this int byteValue, int position) => (byteValue | (1 << position)) & 0xff;
        public static int ClearBit(this int byteValue, int position) => ~(1 << position) & byteValue & 0xff;

        public static int ToWord(this IEnumerable<int> data)
        {
            if (data == null || data.Count() != 2)
            {
                return 0;
            }

            return (data.Last() << 8) | data.First();
        }

        public static int ToSigned(this int byteValue) => (byteValue & (1 << 7)) == 0 ? byteValue : byteValue - 0x100;
    }
}
