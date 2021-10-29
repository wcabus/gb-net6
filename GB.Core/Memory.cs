using System.Runtime.CompilerServices;

namespace GB.Core
{
    internal class Memory
    {
        private readonly int[] _bootrom;

        private readonly Rom _rom;
        
        private readonly int[] _vram = new int[8*1024];
        private readonly int[] _externalRam = new int[8 * 1024];
        private readonly int[] _workRam = new int[8 * 1024];
        private readonly int[] _sprites = new int[0xFE9F - 0xFE00];
        private readonly int[] _io = new int[0xFF7F - 0xFF00];
        private readonly int[] _highRam = new int[0xFFFE - 0xFF80];
        
        private int _interruptEnabled;

        internal static class KnownAddresses
        {
            /// <summary>
            /// Joypad
            /// </summary>
            internal const int JOYPAD = 0xFF00;

            /// <summary>
            /// Serial transfer data
            /// </summary>
            internal const int SB = 0xFF01;

            /// <summary>
            /// Serial transfer control
            /// </summary>
            internal const int SC = 0xFF02;

            /// <summary>
            /// Divider register
            /// </summary>
            internal const int DIV = 0xFF04;

            /// <summary>
            /// Timer counter
            /// </summary>
            internal const int TIMA = 0xFF05;

            /// <summary>
            /// Timer modulo
            /// </summary>
            internal const int TMA = 0xFF06;

            /// <summary>
            /// Timer control
            /// </summary>
            internal const int TAC = 0xFF07;

            /// <summary>
            /// Interrupt flag
            /// </summary>
            internal const int IF = 0xFF0F;

            /// <summary>
            /// Channel 1 sweep register
            /// </summary>
            internal const int NR10 = 0xFF10;

            /// <summary>
            /// Channel 1 sound length/wave pattern duty
            /// </summary>
            internal const int NR11 = 0xFF11;

            /// <summary>
            /// Channel 1 volume envelope
            /// </summary>
            internal const int NR12 = 0xFF12;

            /// <summary>
            /// Channel 1 frequency lo
            /// </summary>
            internal const int NR13 = 0xFF13;

            /// <summary>
            /// Channel 1 frequency hi
            /// </summary>
            internal const int NR14 = 0xFF14;

            /// <summary>
            /// Channel 2 sound length/wave pattern duty
            /// </summary>
            internal const int NR21 = 0xFF16;

            /// <summary>
            /// Channel 2 volume envelope
            /// </summary>
            internal const int NR22 = 0xFF17;

            /// <summary>
            /// Channel 2 frequency lo
            /// </summary>
            internal const int NR23 = 0xFF18;

            /// <summary>
            /// Channel 2 frequency hi
            /// </summary>
            internal const int NR24 = 0xFF19;

            /// <summary>
            /// Channel 3 sound on/off
            /// </summary>
            internal const int NR30 = 0xFF1A;

            /// <summary>
            /// Channel 3 sound length
            /// </summary>
            internal const int NR31 = 0xFF1B;

            /// <summary>
            /// Channel 3 select output level
            /// </summary>
            internal const int NR32 = 0xFF1C;

            /// <summary>
            /// Channel 3 frequency lo
            /// </summary>
            internal const int NR33 = 0xFF1D;

            /// <summary>
            /// Channel 3 frequency hi
            /// </summary>
            internal const int NR34 = 0xFF1E;

            /// <summary>
            /// Channel 4 sound length
            /// </summary>
            internal const int NR41 = 0xFF20;

            /// <summary>
            /// Channel 4 volume envelope
            /// </summary>
            internal const int NR42 = 0xFF21;

            /// <summary>
            /// Channel 4 polynomial counter
            /// </summary>
            internal const int NR43 = 0xFF22;

            /// <summary>
            /// Channel 4 counter/consecutive
            /// </summary>
            internal const int NR44 = 0xFF23;

            /// <summary>
            /// Channel control on/off/volume
            /// </summary>
            internal const int NR50 = 0xFF24;

            /// <summary>
            /// Selection of sound output terminal
            /// </summary>
            internal const int NR51 = 0xFF25;

            /// <summary>
            /// Sound on/off
            /// </summary>
            internal const int NR52 = 0xFF26;

            /// <summary>
            /// Wave pattern start
            /// </summary>
            internal const int WavePatternStart = 0xFF30;

            /// <summary>
            /// Wave pattern end
            /// </summary>
            internal const int WavePatternEnd = 0xFF3F;

            /// <summary>
            /// LCD Control
            /// </summary>
            internal const int LCDC = 0xFF40;

            /// <summary>
            /// LCD Status
            /// </summary>
            internal const int STAT = 0xFF41;

            /// <summary>
            /// LCD Scroll Y
            /// </summary>
            internal const int SCY = 0xFF42;

            /// <summary>
            /// LCD Scroll X
            /// </summary>
            internal const int SCX = 0xFF43;

            /// <summary>
            /// LCD Y coordinate
            /// </summary>
            internal const int LY = 0xFF44;

            /// <summary>
            /// LCD LY compare
            /// </summary>
            internal const int LYC = 0xFF45;

            /// <summary>
            /// DMA Transfer and start address
            /// </summary>
            internal const int DMA = 0xFF46;

            /// <summary>
            /// BG palette data
            /// </summary>
            internal const int BGP = 0xFF47;

            /// <summary>
            /// OBJ palette data 0
            /// </summary>
            internal const int OBP0 = 0xFF48;

            /// <summary>
            /// OBJ palette data 1
            /// </summary>
            internal const int OBP1 = 0xFF49;

            /// <summary>
            /// LCD Window Y position
            /// </summary>
            internal const int WY = 0xFF4A;

            /// <summary>
            /// LCD Window X position
            /// </summary>
            internal const int WX = 0xFF4B;

            /// <summary>
            /// Background color palette specification or background palette index
            /// </summary>
            internal const int BCPS = 0xFF68;
            internal const int BGPI = 0xFF68;

            /// <summary>
            /// Background color palette data or background palette data
            /// </summary>
            internal const int BCPD = 0xFF69;
            internal const int BGPD = 0xFF69;

            /// <summary>
            /// Object color palette specification or object palette index
            /// </summary>
            internal const int OCPS = 0xFF6A;
            internal const int OBPI = 0xFF6A;

            /// <summary>
            /// Object color palette data or object palette data
            /// </summary>
            internal const int OCPD = 0xFF6B;
            internal const int OBPD = 0xFF6B;

            /// <summary>
            /// Interrupt enable
            /// </summary>
            internal const int IE = 0xFFFF;
        }

        public Memory(Rom rom, BootRomType bootRomType)
        {
            _bootrom = bootRomType is BootRomType.CGB or BootRomType.CGB0
                ? BootRom.GBC
                : BootRom.DMG;

            _rom = rom;
            Initialize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize() 
        {
            Write(0xFF00, 0xCF);
            Write(0xFF01, 0x00);
            Write(0xFF02, 0x7E);
            
            Write(KnownAddresses.DIV, 0x00);
            Write(0xFF05, 0x00);
            Write(0xFF06, 0x00);
            Write(0xFF07, 0xF8);
            
            Write(0xFF0F, 0xE1);

            Write(0xFFFF, 0x00);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(int address)
        {
            if (_io[0x50] == 0 && address <= 0x100)
            {
                return _bootrom[address];
            }

            switch (address)
            {
                case int a when a >= 0x0000 && a <= 0x7FFF:
                    return _rom.Read(address);
                case int a when a >= 0x8000 && a <= 0x9FFF:
                    return _vram[address - 0x8000];
                case int a when a >= 0xA000 && a <= 0xBFFF:
                    return _externalRam[address - 0xA000];
                case int a when a >= 0xC000 && a <= 0xDFFF:
                    return _workRam[address - 0xC000];
                case int a when a >= 0xE000 && a <= 0xFDFF:
                    return _workRam[address - 0xE000];
                case int a when a >= 0xFE00 && a <= 0xFE9F:
                    return _sprites[address - 0xFE00];
                case int a when a >= 0xFEA0 && a <= 0xFEFF:
                    return 0; // Unusable
                case int a when a >= 0xFF00 && a <= 0xFF7F:
                    return _io[address - 0xFF00];
                case int a when a >= 0xFF80 && a <= 0xFFFE:
                    return _highRam[address - 0xFF80];
                case 0xFFFF:
                    return _interruptEnabled;
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int address, int value)
        {
            switch (address)
            {
                case int a when a >= 0x8000 && a <= 0x9FFF:
                    _vram[address - 0x8000] = value;
                    break;
                case int a when a >= 0xA000 && a <= 0xBFFF:
                    _externalRam[address - 0xA000] = value;
                    break;
                case int a when a >= 0xC000 && a <= 0xDFFF:
                    _workRam[address - 0xC000] = value;
                    break;
                case int a when a >= 0xFE00 && a <= 0xFE9F:
                    _sprites[address - 0xFE00] = value;
                    break;
                case int a when a >= 0xFF00 && a <= 0xFF7F:
                    WriteIO(address, value);
                    break;
                case int a when a >= 0xFF80 && a <= 0xFFFE:
                    _highRam[address - 0xFF80] = value;
                    break;
                case 0xFFFF:
                    _interruptEnabled = value;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteIO(int address, int value)
        {
            switch (address)
            {
                case KnownAddresses.DIV:
                    _io[KnownAddresses.DIV - 0xFF00] = 0;
                    break;
                default:
                    _io[address - 0xFF00] = value;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncreaseDividerRegister()
        {
            const int address = KnownAddresses.DIV - 0xFF00;
            _io[address]++;
            if (_io[address] > 0xFF)
            {
                _io[address] = 0; // todo set carry?
            }
        }
    }
}
