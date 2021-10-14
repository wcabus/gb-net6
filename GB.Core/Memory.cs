using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB.Core
{
    internal class Memory
    {
        private int[] _bootrom;

        public Memory(Rom rom, BootRomType bootRomType)
        {
            _bootrom = bootRomType is BootRomType.CGB or BootRomType.CGB0
                ? BootRom.GBC
                : BootRom.DMG;
        }

        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case int a when a < 0x100:
                        return _bootrom[index];
                }

                return 0;
            }
        }
    }
}
