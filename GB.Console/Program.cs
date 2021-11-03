using GB.Core;
using GB.Core.Controller;
using GB.Core.Graphics;
using GB.Core.Memory.Cartridge;
using GB.Core.Serial;
using GB.Core.Sound;

var cartridge = Cartridge.FromFile(@"C:\temp\Super Mario Land (World).gb");
if (cartridge == null)
{
    return;
}

var gb = new Gameboy(cartridge, new NullDisplay(), new NullController(), new NullSoundOutput(), new NullSerialEndpoint());
gb.Run(CancellationToken.None);
