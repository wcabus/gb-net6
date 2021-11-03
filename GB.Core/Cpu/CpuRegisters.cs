using System.Runtime.CompilerServices;

namespace GB.Core.Cpu
{
    internal record CpuRegisters
    {
        public int A { get; set; }
        public Flags Flags { get; } = new Flags();

        public int AF
        {
            get => A << 8 | Flags.Byte;
            set
            {
                A = value >> 8;
                Flags.SetFlagsByte(value);
            }
        }

        public int B { get; set; }
        public int C { get; set; }

        public int BC
        {
            get => B << 8 | C;
            set
            {
                B = value >> 8;
                C = value & 0xFF;
            }
        }

        public int D { get; set; }
        public int E { get; set; }

        public int DE
        {
            get => D << 8 | E;
            set
            {
                D = value >> 8;
                E = value & 0xFF;
            }
        }

        public int H { get; set; }
        public int L { get; set; }

        public int HL
        {
            get => H << 8 | L;
            set
            {
                H = value >> 8;
                L = value & 0xFF;
            }
        }

        public int SP { get; set; }
        public int PC { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementPC()
        {
            PC = (PC + 1) & 0xFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementSP()
        {
            SP = (SP + 1) & 0xFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DecrementSP()
        {
            SP = (SP - 1) & 0xFFFF;
        }

        public override string ToString()
        {
            return $"AF={AF:X4} BC={BC:X4} DE={DE:X4} HL={HL:X4} SP={SP:X4} PC={PC:X4} {Flags}";
        }
    }
}
