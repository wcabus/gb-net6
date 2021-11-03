namespace GB.Core.Memory.Cartridge.RTC
{
    internal static class Clock
    {
        public static IClock SystemClock { get; } = new SystemClock();
    }
}
