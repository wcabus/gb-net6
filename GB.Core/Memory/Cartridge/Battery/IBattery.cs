namespace GB.Core.Memory.Cartridge.Battery
{
    internal interface IBattery : IDisposable
    {
        void LoadRam(int[] ram);
        void SaveRam(int[] ram);
        void LoadRamWithClock(int[] ram, long[] clockData);
        void SaveRamWithClock(int[] ram, long[] clockData);
    }
}
