namespace GB.Core
{
    internal class Memory
    {
        private readonly int[] _bootrom;

        public Memory(Rom rom, BootRomType bootRomType)
        {
            _bootrom = bootRomType is BootRomType.CGB or BootRomType.CGB0
                ? BootRom.GBC
                : BootRom.DMG;
        }

        public int Read(int address)
        {
            if (address < 0x100)
            {
                return _bootrom[address];
            }

            return 0;
        }
    }
}
