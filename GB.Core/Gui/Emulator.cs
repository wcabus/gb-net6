using GB.Core.Controller;
using GB.Core.Graphics;
using GB.Core.Memory.Cartridge;
using GB.Core.Serial;
using GB.Core.Sound;

namespace GB.Core.Gui
{
    public class Emulator : IRunnable
    {
        public string? RomPath { get; set; }
        public Gameboy? Gameboy { get; set; }
        
        public IDisplay Display { get; set; } = new NullDisplay();
        public ISoundOutput SoundOutput { get; set; } = new NullSoundOutput();
        public IController Controller { get; set; } = new NullController();
        //public SerialEndpoint SerialEndpoint { get; set; } = new NullSerialEndpoint();
        
        //public GameboyOptions Options { get; set; }
        public bool Active { get; set; }

        private readonly List<Thread> _runnables;

        public Emulator(/*GameboyOptions options*/)
        {
            _runnables = new List<Thread>();
            // Options = options;
        }

        public void Run(CancellationToken token)
        {
            //if (!Options.RomSpecified || !Options.RomFile.Exists)
            //{
            //    throw new ArgumentException("The ROM path doesn't exist: " + Options.RomFile);
            //}
            if (string.IsNullOrEmpty(RomPath))
            {
                throw new ArgumentException("Please choose a ROM.");
            }

            var rom = Cartridge.FromFile(RomPath);
            if (rom is null)
            {
                throw new ArgumentException("The ROM path doesn't exist or points to an invalid ROM file: " + RomPath);
            }

            Gameboy = CreateGameboy(rom);

            if (Display is IRunnable runnableDisplay)
            {
                _runnables.Add(new Thread(() => runnableDisplay.Run(token))
                {
                    Priority = ThreadPriority.AboveNormal
                });
            }

            _runnables.Add(new Thread(() => Gameboy.Run(token))
            {
                Priority = ThreadPriority.AboveNormal
            });

            _runnables.ForEach(t => t.Start());
            Active = true;
        }

        public void Stop(CancellationTokenSource source)
        {
            if (!Active)
            {
                return;
            }

            source.Cancel();
            _runnables.Clear();
        }

        public void TogglePause()
        {
            if (Gameboy != null)
            {
                Gameboy.Paused = !Gameboy.Paused;
            }
        }

        private Gameboy CreateGameboy(Cartridge rom)
        {
            return new Gameboy(rom, Display, Controller, SoundOutput, new NullSerialEndpoint());
        }
    }
}
