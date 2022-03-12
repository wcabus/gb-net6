using GB.Core.Gui;

namespace GB.WebAssembly.Workers
{
    public class EmulatorRunner
    {
        public async Task Run(Emulator emulator, CancellationToken token)
        {
            await emulator.Run(token);
        }
    }
}