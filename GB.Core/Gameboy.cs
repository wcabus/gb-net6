using GB.Core.Controller;
using GB.Core.Cpu;
using GB.Core.Graphics;
using GB.Core.Gui;
using GB.Core.Memory;
using GB.Core.Memory.Cartridge;
using GB.Core.Serial;
using GB.Core.Sound;

namespace GB.Core
{
    public class Gameboy : IRunnable
    {
        public const int TicksPerSec = 4_194_304;

        private readonly Mmu _mmu;
        private readonly Processor _cpu;
        private readonly SpeedMode _speedMode;

        private readonly IDisplay _display;
        private readonly Gpu _gpu;
        private readonly Timer _timer;
        private readonly Dma _dma;
        private readonly Hdma _hdma;
        private readonly Sound.Sound _sound;
        private readonly SerialPort _serialPort;

        private readonly bool _gbc;

        public bool Paused { get; set; }

        public Gameboy(Cartridge cartridge, IDisplay display, IController controller, ISoundOutput soundOutput, ISerialEndpoint serialEndpoint)
        {
            _display = display;
            _gbc = cartridge.IsGameboyColor;
            _speedMode = new SpeedMode();

            var interruptManager = new InterruptManager(_gbc);

            _timer = new Timer(interruptManager, _speedMode);
            _mmu = new Mmu();

            var oamRam = new Ram(0xFE00, 0x00A0);
            
            _dma = new Dma(_mmu, oamRam, _speedMode);
            _gpu = new Gpu(_display, interruptManager, _dma, oamRam, _gbc);
            _hdma = new Hdma(_mmu);
            _sound = new Sound.Sound(soundOutput, _gbc);
            _serialPort = new SerialPort(interruptManager, serialEndpoint, _speedMode);

            _mmu.AddCartridge(cartridge);
            _mmu.AddGpu(_gpu);
            _mmu.AddJoypad(new Joypad(interruptManager, controller));
            _mmu.AddInterruptManager(interruptManager);
            _mmu.AddSerialPort(_serialPort);
            _mmu.AddTimer(_timer);
            _mmu.AddDma(_dma);
            _mmu.AddSound(_sound);

            _mmu.AddFirstRamBank(new Ram(0xC000, 0x1000));
            if (_gbc)
            {
                _mmu.AddSpeedMode(_speedMode);
                _mmu.AddHdma(_hdma);
                _mmu.AddSecondRamBank(new GameboyColorRam());
                _mmu.AddGbcRegisters(new UndocumentedGbcRegisters());
            }
            else
            {
                _mmu.AddSecondRamBank(new Ram(0xD000, 0x1000));
            }

            _mmu.AddHighRam(new Ram(0xFF80, 0x7F));
            _mmu.AddShadowRam(new ShadowAddressSpace(_mmu, 0xE000, 0xC000, 0x1E00));

            _cpu = new Processor(_mmu, interruptManager, _gpu, _display, _speedMode);

            interruptManager.DisableInterrupts(false);

            // uncomment to  skip bootstrap
            // _cpu.InitializeRegisters(_gbc);
            // cartridge.SetByte(0xFF50, 0xFF);
        }

        public void Run(CancellationToken cancellationToken)
        {
            var requestedScreenRefresh = false;
            var lcdDisabled = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Paused)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                var newMode = Tick();
                
                if (newMode.HasValue)
                {
                    _hdma.OnGpuUpdate(newMode.Value);
                }

                if (!lcdDisabled && !_gpu.IsLcdEnabled())
                {
                    lcdDisabled = true;
                    _display.RequestRefresh();
                    _hdma.OnLcdSwitch(false);
                }
                else if (newMode == Gpu.Mode.VBlank)
                {
                    requestedScreenRefresh = true;
                    _display.RequestRefresh();
                }

                if (lcdDisabled && _gpu.IsLcdEnabled())
                {
                    lcdDisabled = false;
                    _display.WaitForRefresh();
                    _hdma.OnLcdSwitch(true);
                }
                else if (requestedScreenRefresh && newMode == Gpu.Mode.OamSearch)
                {
                    requestedScreenRefresh = false;
                    _display.WaitForRefresh();
                }
            }
        }

        private Gpu.Mode? Tick()
        {
            _timer.Tick();

            if (_hdma.IsTransferInProgress())
            {
                _hdma.Tick();
            }
            else
            {
                _cpu.Tick();
            }

            _dma.Tick();
            _sound.Tick();
            _serialPort.Tick();
            return _gpu.Tick();
        }
    }
}
