namespace GB.Core.Memory.Cartridge.Battery
{
    internal class NullBattery : IBattery
    {
        public void LoadRam(int[] ram)
        {
        }

        public void LoadRamWithClock(int[] ram, long[] clockData)
        {
        }

        public void SaveRam(int[] ram)
        {
        }

        public void SaveRamWithClock(int[] ram, long[] clockData)
        {
        }

        public void Dispose() { }
    }
}
