namespace GB.Core
{
    internal class Cpu
    {
        private readonly Memory _memory;

        private const int Speed = 4_194_304;

        public Cpu(Memory memory)
        {
            _memory = memory;

            Registers = new();
            Registers.PC = 0x0000;
        }
        
        public void Run(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ExecuteInstruction();
            }
        }

        private void ExecuteInstruction()
        {
            var opcode = _memory[Registers.PC];
            switch (opcode)
            {
                case 0x00:
                    // NOP
                    Registers.PC++;
                    Tick(4);
                    break;

                case 0xA8:
                    // XOR B
                    Xor(Registers.B);
                    break;
                case 0xA9:
                    // XOR C
                    Xor(Registers.C);
                    break;
                case 0xAA:
                    // XOR D
                    Xor(Registers.D);
                    break;
                case 0xAB:
                    // XOR E
                    Xor(Registers.E);
                    break;
                case 0xAC:
                    // XOR H
                    Xor(Registers.H);
                    break;
                case 0xAD:
                    // XOR L
                    Xor(Registers.L);
                    break;
                case 0xAF:
                    // XOR A
                    Xor(Registers.A);
                    break;

                case 0x01: // LD BC,nnnn
                    Registers.BC = _memory[Registers.PC + 2] << 8 | _memory[Registers.PC + 1];
                    Registers.PC += 3;
                    Tick(12);
                    break;
                case 0x11: // LD DE,nnnn
                    Registers.DE = _memory[Registers.PC + 2] << 8 | _memory[Registers.PC + 1];
                    Registers.PC += 3;
                    Tick(12);
                    break;
                case 0x21: // LD HL,nnnn
                    Registers.HL = _memory[Registers.PC + 2] << 8 | _memory[Registers.PC + 1];
                    Registers.PC += 3;
                    Tick(12);
                    break;
                case 0x31: // LD SP,nnnn
                    Registers.SP = _memory[Registers.PC + 2] << 8 | _memory[Registers.PC + 1];
                    Registers.PC += 3;
                    Tick(12);
                    break;

                case 0xF9:
                    // LD SP,HL
                    Registers.SP = Registers.HL;

                    Registers.PC++;
                    Tick(8);
                    break;

                case 0x37:
                    // SCF
                    Registers.HalfCarry = false;
                    Registers.Subtraction = false;
                    Registers.Carry = true;

                    Registers.PC++;
                    Tick(4);
                    break;

                case 0x3F:
                    // CCF
                    Registers.HalfCarry = false;
                    Registers.Subtraction = false;
                    Registers.Carry ^= true;

                    Registers.PC++;
                    Tick(4);
                    break;
            }
        }

        private void Xor(int value)
        {
            Registers.A ^= value;

            Registers.Zero = Registers.A == 0;
            Registers.Subtraction = false;
            Registers.HalfCarry = false;
            Registers.Carry = false;

            Registers.PC++;
            Tick(4);
        }

        private void Tick(int times)
        {

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
