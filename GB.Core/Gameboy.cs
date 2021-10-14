using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB.Core
{
    public class Gameboy
    {
        public async Task PowerOn(string romPath, CancellationToken cancellation = default)
        {
            var rom = await Rom.FromFile(romPath);
            if (rom is null)
            {
                return;
            }

            var memory = new Memory(rom, BootRomType.DMG);
            var cpu = new Cpu(memory);

            cpu.Run(cancellation);
        }
    }
}
