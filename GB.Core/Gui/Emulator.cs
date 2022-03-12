using System.Runtime.Serialization;
using GB.Core.Controller;
using GB.Core.Graphics;
using GB.Core.Memory.Cartridge;
using GB.Core.Serial;
using GB.Core.Sound;

namespace GB.Core.Gui
{
    [Serializable]
    public class Emulator : IRunnable, ISerializable
    {
        private Cartridge? _cartridge;

        public string? RomPath { get; set; }
        public Stream? RomStream { get; set; }
        public Gameboy? Gameboy { get; set; }
        
        public IDisplay Display { get; set; } = new NullDisplay();
        public ISoundOutput SoundOutput { get; set; } = new NullSoundOutput();
        public IController Controller { get; set; } = new NullController();
        //public SerialEndpoint SerialEndpoint { get; set; } = new NullSerialEndpoint();
        
        //public GameboyOptions Options { get; set; }
        public bool Active { get; set; }

        public Emulator(/*GameboyOptions options*/)
        {
            // Options = options;
        }

        public Emulator(SerializationInfo info, StreamingContext context)
        {
            _cartridge = info.GetValue(nameof(_cartridge), typeof(Cartridge)) as Cartridge;
            RomPath = info.GetString(nameof(RomPath));

            var romStreamBytes = info.GetValue(nameof(RomStream), typeof(byte[])) as byte[];
            if (romStreamBytes is not null)
            {
                RomStream = new MemoryStream(romStreamBytes);
            }

            Gameboy = info.GetValue(nameof(Gameboy), typeof(Gameboy)) as Gameboy;
            Display = info.GetValue(nameof(Display), typeof(IDisplay)) as IDisplay ?? new NullDisplay();
            SoundOutput = info.GetValue(nameof(SoundOutput), typeof(ISoundOutput)) as ISoundOutput ?? new NullSoundOutput();
            Controller = info.GetValue(nameof(Controller), typeof(IController)) as IController ?? new NullController();
            Active = info.GetBoolean(nameof(Active));
        }

        public void SetRomStream(string name, Stream stream)
        {
            RomPath = name;
            RomStream = stream;
        }

        public async Task Run(CancellationToken token)
        {
            //if (!Options.RomSpecified || !Options.RomFile.Exists)
            //{
            //    throw new ArgumentException("The ROM path doesn't exist: " + Options.RomFile);
            //}
            if (string.IsNullOrEmpty(RomPath) && RomStream is null)
            {
                throw new ArgumentException("Please choose a ROM.");
            }

            if (RomStream is not null) 
            {
                // RomPath is the uploaded file name in this case
                _cartridge = await Cartridge.FromStream(RomPath, RomStream);
            }
            else if (!string.IsNullOrEmpty(RomPath))
            {
                _cartridge = await Cartridge.FromFile(RomPath);
            }

            if (_cartridge is null)
            {
                throw new ArgumentException("The ROM path doesn't exist or points to an invalid ROM file: " + RomPath);
            }

            Gameboy = CreateGameboy(_cartridge);

            if (!Display.HasGameloop)
            {
#pragma warning disable CS4014
                Task.Factory.StartNew(() => Gameboy.Run(token), token);
                Task.Factory.StartNew(() => Display.Run(token), token);
#pragma warning restore CS4014
            }
            else
            {
                Gameboy.Run(token);
            }

            Active = true;
        }

        public void Stop(CancellationTokenSource source)
        {
            if (!Active)
            {
                return;
            }

            source.Cancel();
            Active = false;
            
            _cartridge?.SaveRam();
            _cartridge?.Dispose();
        }

        public void ToggleSoundChannel(int channel)
        {
            Gameboy?.ToggleSoundChannel(channel);
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

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(_cartridge), _cartridge);
            info.AddValue(nameof(RomPath), RomPath);

            if (RomStream is null)
            {
                info.AddValue(nameof(RomStream), (byte[]?)null);
            }
            else
            {
                using var ms = new MemoryStream();
                RomStream.CopyTo(ms);
                info.AddValue(nameof(RomStream), ms.ToArray()); // TODO
            }
            
            info.AddValue(nameof(Gameboy), Gameboy);
            info.AddValue(nameof(Display), Display);
            info.AddValue(nameof(SoundOutput), SoundOutput);
            info.AddValue(nameof(Controller), Controller);
            info.AddValue(nameof(Active), Active);
        }
    }
}
