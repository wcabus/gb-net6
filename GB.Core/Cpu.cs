using GB.Core.InstructionSet;

namespace GB.Core
{
    internal class Cpu
    {
        private readonly Memory _memory;
        private int _ticks;
        private const int Speed = 4_194_304;
        private const int InterruptReset = 10; // todo 

        internal int Prefix = 0x00;

        public Cpu(Memory memory)
        {
            _memory = memory;
            Registers = new();
        }
        
        public void Run(CancellationToken cancellationToken)
        {
            Registers.PC = 0x0000;
            _ticks = 0;

            for(;;)
            {
                var opCode = OpCode.Create((Prefix << 8) + _memory.Read(Registers.PC));
                opCode.Execute(this);

                _ticks -= opCode.Ticks(); // Number of ticks can depend on the execution of the action

                if (_ticks <= 0)
                {
                    // check for interrupts, draw, i/o, ...
                    _ticks += InterruptReset;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }

        public CpuRegisters Registers { get; }

        internal record CpuRegisters
        {
            public int A { get; set; }
            public int Flags { get; set; }

            public int AF
            {
                get => A << 8 | Flags;
            }

            public bool Zero
            {
                get => (Flags & 0b0100_0000) != 0;
                set
                {
                    if (value)
                    {
                        Flags = (Flags | 0b0100_0000) & 0xFF;
                    }
                    else
                    {
                        Flags = ~(0b0100_0000) & Flags & 0xFF;
                    }
                }
            }

            public bool Subtraction
            {
                get => (Flags & 0b0010_0000) != 0;
                set
                {
                    if (value)
                    {
                        Flags = (Flags | 0b0010_0000) & 0xFF;
                    }
                    else
                    {
                        Flags = ~(0b0010_0000) & Flags & 0xFF;
                    }
                }
            }

            public bool HalfCarry
            {
                get => (Flags & 0b0001_0000) != 0;
                set
                {
                    if (value)
                    {
                        Flags = (Flags | 0b0001_0000) & 0xFF;
                    }
                    else
                    {
                        Flags = ~(0b0001_0000) & Flags & 0xFF;
                    }
                }
            }

            public bool Carry
            {
                get => (Flags & 0b0000_1000) != 0;
                set
                {
                    if (value)
                    {
                        Flags = (Flags | 0b0000_1000) & 0xFF;
                    }
                    else
                    {
                        Flags = ~(0b0000_1000) & Flags & 0xFF;
                    }
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
        }
    }
}
