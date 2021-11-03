namespace GB.Core.Memory.Cartridge.RTC
{
    internal class SystemClock : IClock
    {
        public long CurrentTimeMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
