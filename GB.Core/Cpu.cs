using GB.Core.InstructionSet;

namespace GB.Core
{
    internal class Cpu
    {
        internal readonly Memory Memory;
        private int _cycles;
        
        private const int Speed = 4_194_304;
        private const int DividerRegisterTicks = Speed / 16384; // 256
        private const int InterruptReset = 10; // todo 

        internal int Prefix = 0x00;

        public Cpu(Memory memory)
        {
            Memory = memory;
            Registers = new();
            OpCode.Cpu = this;
        }
        
        public void Run(CancellationToken cancellationToken)
        {
            Registers.PC = 0x0000;
            _cycles = 0;
            var reduceTicksBy = 0;
            
            var lastTimer = DateTimeOffset.UtcNow.Ticks;
            var now = lastTimer;

            for(;;)
            {
                var opCode = OpCode.Get((Prefix << 8) + Memory.Read(Registers.PC));
                opCode.Execute(this);

                _cycles += opCode.Cycles(); // Number of cycles can depend on the execution of the action

                // check for interrupts, draw, i/o, ...
                if (_cycles >= DividerRegisterTicks)
                {
                    // todo: if stop mode, do nothing
                    Memory.IncreaseDividerRegister();
                    reduceTicksBy = DividerRegisterTicks;
                }

                if (reduceTicksBy > 0)
                {
                    _cycles -= reduceTicksBy;
                    reduceTicksBy = 0;
                }
             
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
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
                set
                {
                    A = value >> 8;
                    Flags = value & 0xF0;
                }
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
