namespace GB.Core.Cpu.InstructionSet
{
    internal record Operand
    {
        private static List<Operand> KnownOperands;

        private Func<CpuRegisters, IAddressSpace, int[], int>? _reader;
        private Action<CpuRegisters, IAddressSpace, int[], int>? _writer;

        static Operand() 
        {
            KnownOperands = new()
            {
                new Operand("A").SetHandlers((r, m, args) => r.A, (r, m, args, value) => r.A = value),
                new Operand("B").SetHandlers((r, m, args) => r.B, (r, m, args, value) => r.B = value),
                new Operand("C").SetHandlers((r, m, args) => r.C, (r, m, args, value) => r.C = value),
                new Operand("D").SetHandlers((r, m, args) => r.D, (r, m, args, value) => r.D = value),
                new Operand("E").SetHandlers((r, m, args) => r.E, (r, m, args, value) => r.E = value),
                new Operand("H").SetHandlers((r, m, args) => r.H, (r, m, args, value) => r.H = value),
                new Operand("L").SetHandlers((r, m, args) => r.L, (r, m, args, value) => r.L = value),

                new Operand("AF", 0, false, DataType.d16)
                    .SetHandlers((r, m, args) => r.AF, (r, m, args, value) => r.AF = value),

                new Operand("BC", 0, false, DataType.d16)
                    .SetHandlers((r, m, args) => r.BC, (r, m, args, value) => r.BC = value),

                new Operand("DE", 0, false, DataType.d16)
                    .SetHandlers((r, m, args) => r.DE, (r, m, args, value) => r.DE = value),

                new Operand("HL", 0, false, DataType.d16)
                    .SetHandlers((r, m, args) => r.HL, (r, m, args, value) => r.HL = value),

                new Operand("SP", 0, false, DataType.d16)
                    .SetHandlers((r, m, args) => r.SP, (r, m, args, value) => r.SP = value),

                new Operand("PC", 0, false, DataType.d16)
                    .SetHandlers((r, m, args) => r.PC, (r, m, args, value) => r.PC = value),

                new Operand("d8", 1, false, DataType.d8)
                    .SetHandlers((r, m, args) => args[0]),

                new Operand("d16", 2, false, DataType.d16)
                    .SetHandlers((r, m, args) => (args[1] << 8) | args[0]),

                new Operand("r8", 1, false, DataType.r8)
                    .SetHandlers((r, m, args) => (args[0] & (1 << 7)) == 0 ? args[0] : args[0] - 0x100),

                new Operand("a16", 2, false, DataType.d16)
                    .SetHandlers((r, m, args) => (args[1] << 8) | args[0]),

                new Operand("(BC)", 0, true, DataType.d8)
                    .SetHandlers((r, m, args) => m.GetByte(r.BC), (r, m, args, value) => m.SetByte(r.BC, value)),

                new Operand("(DE)", 0, true, DataType.d8)
                    .SetHandlers((r, m, args) => m.GetByte(r.DE), (r, m, args, value) => m.SetByte(r.DE, value)),

                new Operand("(HL)", 0, true, DataType.d8)
                    .SetHandlers((r, m, args) => m.GetByte(r.HL), (r, m, args, value) => m.SetByte(r.HL, value)),

                new Operand("(a8)", 1, true, DataType.d8)
                    .SetHandlers((r, m, args) => m.GetByte(0xFF00 | args[0]), (r, m, args, value) => m.SetByte(0xFF00 | args[0], value)),

                new Operand("(a16)", 2, true, DataType.d8)
                    .SetHandlers((r, m, args) => m.GetByte((args[1] << 8) | args[0]), (r, m, args, value) => m.SetByte((args[1] << 8) | args[0], value)),

                new Operand("(C)", 0, true, DataType.d8)
                    .SetHandlers((r, m, args) => m.GetByte(0xFF00 | r.C), (r, m, args, value) => m.SetByte(0xFF00 | r.C, value))
            };
        }

        public static Operand Parse(string name)
        {
            return KnownOperands.FirstOrDefault(x => x.Name == name) 
                ?? throw new ArgumentException("Unknown operand", nameof(name));
        }

        private Operand(string name) : this(name, 0, false, DataType.d8)
        {
        }

        private Operand(string name, int bytes, bool accessesMemory, DataType dataType)
        {
            Name = name;
            Bytes = bytes;
            AccessesMemory = accessesMemory;
            DataType = dataType;
        }

        public string Name { get; }
        public int Bytes { get; }
        public bool AccessesMemory { get; }
        public DataType DataType { get; }

        public int Read(CpuRegisters registers, IAddressSpace addressSpace, int[] args)
        {
            return _reader?.Invoke(registers, addressSpace, args) ?? 
                throw new InvalidOperationException("Reader not set!");
        }

        public void Write(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int value)
        {
            _writer?.Invoke(registers, addressSpace, args, value);
        }

        private Operand SetHandlers(Func<CpuRegisters, IAddressSpace, int[], int> reader, Action<CpuRegisters, IAddressSpace, int[], int>? writer = null)
        {
            _reader = reader;
            _writer = writer;
            return this;
        }
    }
}