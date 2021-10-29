using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace GB.Core.InstructionSet;

internal class OpCode
{
    private readonly int _opCode;
    private readonly string _mnemonic;
    private readonly int _bytes;
    private readonly int _cycles;
    private readonly int? _cyclesWhenActionNotTaken;
    private readonly Operand[] _operands;
    private readonly bool _immediate;
    private readonly InstructionFlags _flags;

    private string? _toString;

    // Operational fields
    private bool _actionTaken = true;
    private bool _reduceCyclesByFour = false;

    private static readonly Dictionary<int, OpCode> OpCodes = new();

    static OpCode()
    {
        InitializeOpCodes();
    }

    public static void InitializeOpCodes()
    {
        if (OpCodes.Count > 0)
        {
            return;
        }

        var propertyName = "";
        var cbPrefixed = false;
        var depth = 0;
        var arrayPos = -1;
        var inArray = false;

        var opCodeBytes = -1;
        var mnemonic = "";
        var bytes = 0;
        var cycles = 0;
        int? cyclesIfActionNotTaken = null;
        var immediate = false;

        // operand
        var inOperandArray = false;
        var operandName = "";
        var operandImmediate = false;
        var operandIncrementOrDecrement = 0;
        int? operandBytes = null;
        List<Operand> operands = new();

        // flags
        var zFlag = InstructionFlag.Unchanged;
        var nFlag = InstructionFlag.Unchanged;
        var hFlag = InstructionFlag.Unchanged;
        var cFlag = InstructionFlag.Unchanged;

        OpCode opCode;

        var reader = new Utf8JsonReader(CoreResources.opcodes.AsSpan());
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    propertyName = reader.GetString() ?? "";
                    switch (propertyName)
                    {
                        case "cbprefixed":
                            cbPrefixed = true;
                            break;
                        case "operands":
                            inOperandArray = true;
                            break;
                    }
                    break;
                case JsonTokenType.String:
                    var text = reader.GetString() ?? "";
                    switch (propertyName)
                    {
                        case "mnemonic":
                            mnemonic = text;
                            break;
                        case "name":
                            operandName = text;
                            break;
                        case "Z":
                        case "N":
                        case "H":
                        case "C":
                            var flag = text switch
                            {
                                "-" => InstructionFlag.Unchanged,
                                "0" => InstructionFlag.Zero,
                                "1" => InstructionFlag.One,
                                _ => InstructionFlag.Determine
                            };
                            switch (propertyName)
                            {
                                case "Z":
                                    zFlag = flag;
                                    break;
                                case "N":
                                    nFlag = flag;
                                    break;
                                case "H":
                                    hFlag = flag;
                                    break;
                                case "C":
                                    cFlag = flag;
                                    break;
                            }
                            break;
                    }

                    break;
                case JsonTokenType.Number:
                    var number = reader.GetInt32();
                    switch (propertyName)
                    {
                        case "bytes":
                            if (inOperandArray)
                            {
                                operandBytes = number;
                            }
                            else
                            {
                                bytes = number;
                            }
                            break;
                        case "cycles":
                            if (arrayPos == 0)
                            {
                                cycles = number;
                            }
                            else if (arrayPos == 1)
                            {
                                cyclesIfActionNotTaken = number;
                            }
                            arrayPos++;
                            break;
                    }
                    break;
                case JsonTokenType.True:
                    if (inOperandArray)
                    {
                        switch (propertyName)
                        {
                            case "immediate":
                                operandImmediate = true;
                                break;
                            case "decrement":
                                operandIncrementOrDecrement = -1;
                                break;
                            case "increment":
                                operandIncrementOrDecrement = 1;
                                break;
                        }
                    }
                    else
                    {
                        immediate = true;
                    }
                    break;
                case JsonTokenType.False:
                    if (inOperandArray)
                    {
                        operandImmediate = false;
                    }
                    else
                    {
                        immediate = false;
                    }
                    break;
                case JsonTokenType.StartArray:
                    arrayPos = 0;
                    inArray = true;
                    break;
                case JsonTokenType.EndArray:
                    inOperandArray = false;
                    inArray = false;
                    arrayPos = -1;
                    break;
                case JsonTokenType.StartObject:
                    if (propertyName.StartsWith("0x", StringComparison.Ordinal))
                    {
                        opCodeBytes = Convert.ToInt32(propertyName[2..], 16);
                        if (cbPrefixed)
                        {
                            opCodeBytes = (0xCB << 8) + opCodeBytes;
                        }
                    }

                    depth++;
                    break;
                case JsonTokenType.EndObject:
                    if (inArray)
                    {
                        arrayPos++;
                    }

                    depth--;

                    if (inOperandArray)
                    {
                        // finished reading an operand for the current opcode
                        operands.Add(new Operand(operandName, operandImmediate, operandBytes, operandIncrementOrDecrement == 1, operandIncrementOrDecrement == -1));

                        // Reset
                        operandName = "";
                        operandImmediate = false;
                        operandBytes = null;
                        operandIncrementOrDecrement = 0;
                    }

                    if (depth == 2)
                    {
                        // finished reading an opcode and all its details
                        if (cyclesIfActionNotTaken.HasValue)
                        {
                            opCode = new OpCode(opCodeBytes, mnemonic, bytes, cycles, cyclesIfActionNotTaken.Value, operands, immediate, new InstructionFlags(zFlag, nFlag, hFlag, cFlag));
                        }
                        else 
                        {
                            opCode = new OpCode(opCodeBytes, mnemonic, bytes, cycles, operands, immediate, new InstructionFlags(zFlag, nFlag, hFlag, cFlag));
                        }
                        
                        OpCodes.Add(opCodeBytes, opCode);

                        // Reset
                        opCodeBytes = -1;
                        bytes = 0;
                        cycles = 0;
                        cyclesIfActionNotTaken = null;
                        mnemonic = "";
                        immediate = false;
                        operands = new();
                        zFlag = InstructionFlag.Unchanged;
                        nFlag = InstructionFlag.Unchanged;
                        hFlag = InstructionFlag.Unchanged; 
                        cFlag = InstructionFlag.Unchanged;
                    }
                    break;
            }
        }
    }

    private OpCode(int opCode, string mnemonic, int bytes, int cycles, IReadOnlyCollection<Operand> operands, bool immediate, InstructionFlags flags) 
        : this(opCode, mnemonic, bytes, cycles, null, operands, immediate, flags)
    {
    }

    private OpCode(int opCode, string mnemonic, int bytes, int cycles, int? cyclesWhenActionNotTaken, IReadOnlyCollection<Operand> operands, bool immediate, InstructionFlags flags)
    {
        _opCode = opCode;
        _mnemonic = mnemonic;
        _bytes = bytes;
        _cycles = cycles;
        _cyclesWhenActionNotTaken = cyclesWhenActionNotTaken;
        _operands = operands.ToArray();
        _immediate = immediate;
        _flags = flags;
    }

    public override string ToString()
    {
        if (_toString is null)
        {
            var sb = new StringBuilder(_mnemonic);
            var operandCount = 0;
            foreach (var operand in _operands)
            {
                if (operandCount > 0)
                {
                    sb.Append(',');
                }
                sb.Append(' ');
                if (operand.Immediate)
                {
                    sb.AppendFormat("{0}{1}", operand.Name, operand.Increment ? "+" : operand.Decrement ? "-" : "");
                }
                else
                {
                    sb.AppendFormat("({0}{1})", operand.Name, operand.Increment ? "+" : operand.Decrement ? "-" : "");
                }

                operandCount++;
            }
            
            _toString = sb.ToString();
        }

        return _toString;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpCode Get(int opCode)
    {
        return OpCodes[opCode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Cycles() 
    {
        var cycles = _actionTaken ? _cycles : _cyclesWhenActionNotTaken.GetValueOrDefault(_cycles);
        cycles -= (_reduceCyclesByFour ? 4 : 0);
        _reduceCyclesByFour = false;

        return cycles < 0 ? 0 : cycles;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Bytes() => _bytes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(Cpu cpu)
    {
        if (_opCode == 0xCB)
        {
            cpu.Prefix = 0xCB;
            cpu.Registers.PC += _bytes;
            return;
        }
                
#if DEBUG
        Console.WriteLine(ToString());
#endif

        var bytes = _bytes;
        if (cpu.Prefix == 0xCB)
        {
            bytes--;
            _reduceCyclesByFour = true;
        }
        cpu.Prefix = 0x00;
        
        switch (_mnemonic)
        {
            case "NOP": // No operation
                break;
            case "STOP": // Stop CPU
                // cpu.Stopped = true;
                break;
            case "HALT": // Halt CPU
                // cpu.Halted = true;
                break;
            case "DI": // Disable interrupts
                // cpu.InterruptsEnabled = false;
                break;
            case "EI": // Enable interrupts
                // cpu.InterruptsEnabled = true;
                break;
            case "CCF": // Complement Carry Flag
                cpu.Registers.Carry = cpu.Registers.Carry ^ true;
                break;
            case "SCF": // Set Carry Flag
                cpu.Registers.Carry = true;
                break;
            case "CPL": // Complement A register
                cpu.Registers.A = (byte)~cpu.Registers.A;
                cpu.Registers.Subtraction = true;
                cpu.Registers.HalfCarry = true;
                break;
            case "DAA": // Decimal Adjust Accumulator
                BcdAdjust(cpu);
                break;
            case "LD":
            case "LDH":
                Load(cpu);
                break;
            case "ADC": // Add with carry two registers, memory to register, direct to memory, ...
                Add(cpu, true);
                break;
            case "ADD": // Add two registers, memory to register, direct to memory, ...
                Add(cpu, false);
                break;
            case "AND": // AND two registers, memory to register, direct to memory, ...
                And(cpu);
                break;
            case "CP": // Compare two registers, memory to register, direct to memory, ...
                Compare(cpu);
                break;
            case "OR": // OR two registers, memory to register, direct to memory, ...
                Or(cpu);
                break;
            case "SBC": // Subtract with carry two registers, memory to register, direct to memory, ...
                Subtract(cpu, true);
                break;
            case "SUB": // Subtract two registers, memory to register, direct to memory, ...
                Subtract(cpu, false);
                break;
            case "XOR": // XOR two registers, memory to register, direct to memory, ...
                Xor(cpu);
                break;
            // Stack
            case "PUSH":
                Push(cpu);
                break;
            case "POP":
                Pop(cpu);
                break;
            // Increment, decrement
            case "INC":
                Increment(cpu);
                break;
            case "DEC":
                Decrement(cpu);
                break;
            case "RLCA":
                RotateLeft(cpu, new Operand("A", true, null, false, false), false);
                break;
            case "RLA":
                RotateLeft(cpu, new Operand("A", true, null, false, false), true);
                break;
            case "RLC":
                RotateLeft(cpu, _operands[0], false);
                break;
            case "RL":
                RotateLeft(cpu, _operands[0], true);
                break;
            case "RRCA":
                RotateRight(cpu, new Operand("A", true, null, false, false), false);
                break;
            case "RRA":
                RotateRight(cpu, new Operand("A", true, null, false, false), true);
                break;
            case "RRC":
                RotateRight(cpu, _operands[0], false);
                break;
            case "RR":
                RotateRight(cpu, _operands[0], true);
                break;
            case "SLA":
                ShiftLeft(cpu);
                break;
            case "SRA":
                ShiftRight(cpu, true);
                break;
            case "SRL":
                ShiftRight(cpu, false);
                break;
            case "SWAP":
                Swap(cpu);
                break;
            case "BIT":
                BitTest(cpu);
                break;
            case "SET":
                SetBit(cpu);
                break;
            case "RES":
                ResetBit(cpu);
                break;
            case "JP":
                Jump(cpu);
                bytes = 0;
                break;
            case "JR":
                JumpRelative(cpu);
                break;
            case "CALL":
                Call(cpu, bytes);
                bytes = 0;
                break;
            case "RET":
            case "RETI":
                Return(cpu);
                bytes = 0;
                break;
            case "RST":
                JumpReset(cpu, bytes);
                bytes = 0;
                break;
        }

        cpu.Registers.PC += bytes; // could be different when jumping, this is the default for just increasing the PC!
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BcdAdjust(Cpu cpu)
    {
        var a = cpu.Registers.A;
        var carry = cpu.Registers.Carry;
        var halfCarry = cpu.Registers.HalfCarry;

        var ls = (a & 0xF);
        var ms = ((a >> 4) & 0xF);

        if (ls > 0x9 || halfCarry)
        {
            ls += 0x6;
            if (ls > 0xF)
            {
                ls -= 0xF;
                ms++;
                if (ms > 0xF)
                {
                    ms = 0;
                    cpu.Registers.Carry = true;
                }
            }
        }
        
        if (ms > 0x9 || carry)
        {
            ms += 0x6;
            cpu.Registers.Carry = true;
            if (ms > 0xF)
            {
                ms -= 0xF;
            }
        }

        cpu.Registers.A = (ms << 4) + ls;
    }

    /// <summary>
    /// LD target, source
    /// LDH target, source
    /// LDHL target, source
    /// Where source or target can be:
    /// * an 8 or 16-bit register
    /// * an indirect address (register points to memory location)
    /// * a direct address
    /// * indexed address (I/O range FF00 + direct offset / register offset)
    /// </summary>
    /// <param name="cpu"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Load(Cpu cpu)
    {
        var source = _operands[1];
        var sourceData = ReadSource(source, cpu);

        if (_operands.Length == 3) // LDHL SP,n
        {
            var sourceData2 = (sbyte)ReadSource(_operands[2], cpu);
            sourceData += sourceData2;
        }

        var target = _operands[0];
        WriteTarget(target, sourceData, cpu);

        if (_opCode == 0x00F8)
        {
            UpdateFlags(cpu, false, false, (sourceData & 0x10) != 0, (sourceData & 0x100) != 0);
        }
    }

    /// <summary>
    /// ADC target, source
    /// ADD target, source
    /// Where source or target can be:
    /// * an 8 or 16-bit register
    /// * an indirect address (register points to memory location)
    /// * a direct address
    /// * indexed address (I/O range FF00 + direct offset / register offset)
    /// </summary>
    /// <param name="cpu"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Add(Cpu cpu, bool withCarry = false)
    {
        var source = _operands[1];
        var sourceData = ReadSource(source, cpu);

        if (_opCode == 0xE8)
        {
            // ADD SP, r8
            sourceData = (sbyte)sourceData;
        }

        var target = _operands[0];
        var data = ReadSource(target, cpu);
        var bit3Set = ((data >> 3) & 1) == 1;
        var bit11Set = ((data >> 11) & 1) == 1;

        data += sourceData;
        var bit3Set2 = ((data >> 3) & 1) == 1;
        var bit11Set2 = ((data >> 11) & 1) == 1;

        data += (withCarry && cpu.Registers.Carry ? 1 : 0);
        var bit3Set3 = ((data >> 3) & 1) == 1;
        var bit11Set3 = ((data >> 11) & 1) == 1;

        switch (target.Name)
        {
            case "A":
                cpu.Registers.A = data & 0xFF;
                UpdateFlags(cpu, (data & 0xFF) == 0, false, bit3Set && (!bit3Set2 || !bit3Set3), (data >> 8) != 0);
                break;
            case "HL":
                cpu.Registers.HL = data & 0xFFFF;
                UpdateFlags(cpu, null, false, bit11Set && (!bit11Set2 || !bit11Set3), (data >> 17) != 0);
                break;
            case "SP":
                cpu.Registers.SP = data & 0xFFFF;
                UpdateFlags(cpu, false, false, bit11Set && (!bit11Set2 || !bit11Set3), (data >> 17) != 0);
                break;
        }
    }

    /// <summary>
    /// SBC target, source
    /// SUB target, source
    /// Where source or target can be:
    /// * an 8 or 16-bit register
    /// * an indirect address (register points to memory location)
    /// * a direct address
    /// * indexed address (I/O range FF00 + direct offset / register offset)
    /// </summary>
    /// <param name="cpu"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Subtract(Cpu cpu, bool withCarry = false)
    {
        var source = _operands[0];
        var sourceData = (byte)ReadSource(source, cpu);

        var data = (byte)cpu.Registers.A;
        var bit4Set = ((data >> 4) & 1) == 1;
        var bit8Set = ((data >> 8) & 1) == 1;

        data = (byte)(data - sourceData - (withCarry && cpu.Registers.Carry ? 1 : 0));
        var bit4Set2 = ((data >> 4) & 1) == 1;
        var bit8Set2 = ((data >> 8) & 1) == 1;

        cpu.Registers.A = data;
        UpdateFlags(cpu, data == 0, true, bit4Set && !bit4Set2, !bit8Set && bit8Set2);
    }

    /// <summary>
    /// AND source
    /// </summary>
    /// <param name="cpu"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void And(Cpu cpu)
    {
        var source = _operands[0];
        var sourceData = (byte)ReadSource(source, cpu);

        cpu.Registers.A = (byte)(cpu.Registers.A & sourceData);
        UpdateFlags(cpu, cpu.Registers.A == 0, false, true, false);
    }

    /// <summary>
    /// OR source
    /// </summary>
    /// <param name="cpu"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Or(Cpu cpu)
    {
        var source = _operands[0];
        var sourceData = (byte)ReadSource(source, cpu);

        cpu.Registers.A = (byte)(cpu.Registers.A | sourceData);
        UpdateFlags(cpu, cpu.Registers.A == 0, false, false, false);
    }

    /// <summary>
    /// XOR source
    /// </summary>
    /// <param name="cpu"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Xor(Cpu cpu)
    {
        var source = _operands[0];
        var sourceData = (byte)ReadSource(source, cpu);

        cpu.Registers.A = (byte)(cpu.Registers.A ^ sourceData);
        UpdateFlags(cpu, cpu.Registers.A == 0, false, false, false);
    }

    /// <summary>
    /// CP source
    /// </summary>
    /// <param name="cpu"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Compare(Cpu cpu)
    {
        var source = _operands[0];
        var sourceData = (byte)ReadSource(source, cpu);

        var data = (byte)cpu.Registers.A;
        var bit4Set = ((data >> 4) & 1) == 1;
        var bit8Set = ((data >> 8) & 1) == 1;

        var result = (byte)(data - sourceData);
        var bit4Set2 = ((result >> 4) & 1) == 1;
        var bit8Set2 = ((result >> 8) & 1) == 1;

        UpdateFlags(cpu, result == 0, true, bit4Set && !bit4Set2, !bit8Set && bit8Set2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Push(Cpu cpu)
    {
        var source = _operands[0];
        var data = ReadSource(source, cpu) & 0xFFFF;

        cpu.Memory.Write(cpu.Registers.SP - 1, data >> 8);
        cpu.Memory.Write(cpu.Registers.SP - 2, data & 0xFF);
        cpu.Registers.SP -= 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Pop(Cpu cpu)
    {
        var lsb = cpu.Memory.Read(cpu.Registers.SP);
        var hsb = cpu.Memory.Read(cpu.Registers.SP + 1);

        cpu.Registers.SP += 2;
        var target = _operands[0];
        WriteTarget(target, (hsb << 8) + lsb, cpu);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Increment(Cpu cpu)
    {
        var operand = _operands[0];
        var data = ReadSource(operand, cpu);
        var newData = (data + 1) & 0xFF;

        WriteTarget(operand, newData, cpu);

        if (operand.Name.Length == 1 || !operand.Immediate)
        {
            UpdateFlags(cpu, newData == 0, false, (data & 0x0F) == 0x0F);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Decrement(Cpu cpu)
    {
        var operand = _operands[0];
        var data = ReadSource(operand, cpu);
        var newData = (data - 1) & 0xFF;

        WriteTarget(operand, newData, cpu);

        if (operand.Name.Length == 1 || !operand.Immediate)
        {
            UpdateFlags(cpu, newData == 0, true, (data & 0x0F) == 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateLeft(Cpu cpu, Operand operand, bool useCarry)
    {
        var data = ReadSource(operand, cpu);

        var newCarry = (data >> 7) == 1;
        var carry = useCarry ? cpu.Registers.Carry : newCarry;
        
        data = (data << 1) + (carry ? 1 : 0);
        
        WriteTarget(operand, data, cpu);
        
        var zero = operand.Name == "A" ? false : ((data & 0xFF) == 0);
        UpdateFlags(cpu, zero, false, false, newCarry);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateRight(Cpu cpu, Operand operand, bool useCarry)
    {
        var data = ReadSource(operand, cpu);

        var newCarry = (data & 0x01) == 1;
        var carry = useCarry ? cpu.Registers.Carry : newCarry;

        data = (data >> 1) + (carry ? 0x80 : 0);

        WriteTarget(operand, data, cpu);
        
        var zero = operand.Name == "A" ? false : ((data & 0xFF) == 0);
        UpdateFlags(cpu, zero, false, false, newCarry);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ShiftLeft(Cpu cpu)
    {
        var operand = _operands[0];
        var data = ReadSource(operand, cpu);

        var newCarry = (data >> 7) == 1;
        data <<= 1;

        WriteTarget(operand, data, cpu);
        UpdateFlags(cpu, (data & 0xFF) == 0, false, false, newCarry);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ShiftRight(Cpu cpu, bool keepLeftmostBit)
    {
        var operand = _operands[0];
        var data = ReadSource(operand, cpu);

        var leftmostBitSet = (data >> 7) == 1;
        var newCarry = (data & 0x01) == 1;
        data >>= 1;

        if (keepLeftmostBit && leftmostBitSet)
        {
            data += 0x80;
        }

        WriteTarget(operand, data, cpu);
        UpdateFlags(cpu, (data & 0xFF) == 0, false, false, newCarry);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Swap(Cpu cpu)
    {
        var operand = _operands[0];
        var data = ReadSource(operand, cpu);

        var low = data & 0x0F;
        var high = (data & 0xF0) >> 4;
        data = (low << 4) + high;

        WriteTarget(operand, data, cpu);
        UpdateFlags(cpu, (data & 0xFF) == 0, false, false, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BitTest(Cpu cpu)
    {
        var bitIndex = _operands[0].Name;
        var operand = _operands[1];
        var data = ReadSource(operand, cpu);

        var bitSet = false;
        switch (bitIndex)
        {
            case "0":
                bitSet = (data & 0x01) == 0x01;
                break;
            case "1":
                bitSet = (data & 0x02) == 0x02;
                break;
            case "2":
                bitSet = (data & 0x04) == 0x04;
                break;
            case "3":
                bitSet = (data & 0x08) == 0x08;
                break;
            case "4":
                bitSet = (data & 0x10) == 0x10;
                break;
            case "5":
                bitSet = (data & 0x20) == 0x20;
                break;
            case "6":
                bitSet = (data & 0x40) == 0x40;
                break;
            case "7":
                bitSet = (data & 0x80) == 0x80;
                break;
        }

        UpdateFlags(cpu, !bitSet, false, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBit(Cpu cpu)
    {
        var bitIndex = _operands[0].Name;
        var operand = _operands[1];
        var data = ReadSource(operand, cpu);

        switch (bitIndex)
        {
            case "0":
                data |= 0x01;
                break;
            case "1":
                data |= 0x02;
                break;
            case "2":
                data |= 0x04;
                break;
            case "3":
                data |= 0x08;
                break;
            case "4":
                data |= 0x10;
                break;
            case "5":
                data |= 0x20;
                break;
            case "6":
                data |= 0x40;
                break;
            case "7":
                data |= 0x80;
                break;
        }

        WriteTarget(operand, data, cpu);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResetBit(Cpu cpu)
    {
        var bitIndex = _operands[0].Name;
        var operand = _operands[1];
        var data = ReadSource(operand, cpu);

        switch (bitIndex)
        {
            case "0":
                data &= 0xFE;
                break;
            case "1":
                data &= 0xFD;
                break;
            case "2":
                data &= 0xFB;
                break;
            case "3":
                data &= 0xF7;
                break;
            case "4":
                data &= 0xEF;
                break;
            case "5":
                data &= 0xDF;
                break;
            case "6":
                data &= 0xBF;
                break;
            case "7":
                data &= 0x7F;
                break;
        }

        WriteTarget(operand, data, cpu);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Jump(Cpu cpu)
    {
        var address = ReadSource(_operands.Last(), cpu);
        var execute = true;

        if (_operands.Length == 2)
        {
            // First operand decides whether the action should take place
            switch (_operands[0].Name)
            {
                case "NZ":
                    execute = !cpu.Registers.Zero;
                    break;
                case "Z":
                    execute = cpu.Registers.Zero;
                    break;
                case "NC":
                    execute = !cpu.Registers.Carry;
                    break;
                case "C":
                    execute = cpu.Registers.Carry;
                    break;
            }
        }

        if (execute)
        {
            _actionTaken = true;
            cpu.Registers.PC = address & 0xFFFF;
            return;
        }

        _actionTaken = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void JumpRelative(Cpu cpu)
    {
        var offset = ((sbyte)ReadSource(_operands.Last(), cpu));
        var execute = true;

        if (_operands.Length == 2)
        {
            // First operand decides whether the action should take place
            switch (_operands[0].Name)
            {
                case "NZ":
                    execute = !cpu.Registers.Zero;
                    break;
                case "Z":
                    execute = cpu.Registers.Zero;
                    break;
                case "NC":
                    execute = !cpu.Registers.Carry;
                    break;
                case "C":
                    execute = cpu.Registers.Carry;
                    break;
            }
        }

        if (execute)
        {
            _actionTaken = true;
            cpu.Registers.PC = (cpu.Registers.PC + offset) & 0xFFFF;
            return;
        }

        _actionTaken = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Call(Cpu cpu, int instructionLength)
    {
        var address = ReadSource(_operands.Last(), cpu);
        var execute = true;

        if (_operands.Length == 2)
        {
            // First operand decides whether the action should take place
            switch (_operands[0].Name)
            {
                case "NZ":
                    execute = !cpu.Registers.Zero;
                    break;
                case "Z":
                    execute = cpu.Registers.Zero;
                    break;
                case "NC":
                    execute = !cpu.Registers.Carry;
                    break;
                case "C":
                    execute = cpu.Registers.Carry;
                    break;
            }
        }

        if (execute)
        {
            _actionTaken = true;
            var pcWhenReturning = cpu.Registers.PC + instructionLength;

            cpu.Memory.Write(cpu.Registers.SP - 1, pcWhenReturning >> 8);
            cpu.Memory.Write(cpu.Registers.SP - 2, pcWhenReturning & 0xFF);
            cpu.Registers.PC = address & 0xFFFF;
            cpu.Registers.SP -= 2;
            return;
        }

        _actionTaken = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Return(Cpu cpu)
    {
        var execute = true;

        if (_operands.Length == 1)
        {
            // First operand decides whether the action should take place
            switch (_operands[0].Name)
            {
                case "NZ":
                    execute = !cpu.Registers.Zero;
                    break;
                case "Z":
                    execute = cpu.Registers.Zero;
                    break;
                case "NC":
                    execute = !cpu.Registers.Carry;
                    break;
                case "C":
                    execute = cpu.Registers.Carry;
                    break;
            }
        }

        if (execute)
        {
            _actionTaken = true;

            cpu.Registers.PC = cpu.Memory.Read(cpu.Registers.SP) + (cpu.Memory.Read(cpu.Registers.SP + 1) << 8);
            cpu.Registers.SP += 2;
            return;
        }

        _actionTaken = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void JumpReset(Cpu cpu, int instructionLength)
    {
        var newPc = 0x0000;
        switch (_operands[0].Name)
        {
            case "08H":
                newPc = 0x08;
                break;
            case "10H":
                newPc = 0x10;
                break;
            case "18H":
                newPc = 0x18;
                break;
            case "20H":
                newPc = 0x20;
                break;
            case "28H":
                newPc = 0x28;
                break;
            case "30H":
                newPc = 0x30;
                break;
            case "38H":
                newPc = 0x38;
                break;
        }

        var pcWhenReturning = cpu.Registers.PC + instructionLength;

        cpu.Memory.Write(cpu.Registers.SP - 1, pcWhenReturning >> 8);
        cpu.Memory.Write(cpu.Registers.SP - 2, pcWhenReturning & 0xFF);
        cpu.Registers.PC = newPc;
        cpu.Registers.SP -= 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteTarget(Operand target, int data, Cpu cpu)
    {
        if (target.Immediate)
        {
            switch (target.Name)
            {
                case "A":
                    cpu.Registers.A = data & 0xFF;
                    break;
                case "B":
                    cpu.Registers.B = data & 0xFF;
                    break;
                case "C":
                    cpu.Registers.C = data & 0xFF;
                    break;
                case "D":
                    cpu.Registers.D = data & 0xFF;
                    break;
                case "E":
                    cpu.Registers.E = data & 0xFF;
                    break;
                case "H":
                    cpu.Registers.H = data & 0xFF;
                    break;
                case "L":
                    cpu.Registers.L = data & 0xFF;
                    break;
                case "BC":
                    cpu.Registers.BC = data & 0xFFFF;
                    break;
                case "AF":
                    cpu.Registers.AF = data & 0xFFFF;
                    break;
                case "DE":
                    cpu.Registers.DE = data & 0xFFFF;
                    break;
                case "HL":
                    cpu.Registers.HL = data & 0xFFFF;
                    break;
                case "SP":
                    cpu.Registers.SP = data & 0xFFFF;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid target operand '{target.Name}'!");
            }
            return;
        }

        // Indirect
        switch (target.Name)
        {
            case "BC":
                cpu.Memory.Write(cpu.Registers.BC & 0xFFFF, data & 0xFF);
                break;
            case "DE":
                cpu.Memory.Write(cpu.Registers.DE & 0xFFFF, data & 0xFF);
                break;
            case "HL":
                cpu.Memory.Write(cpu.Registers.HL & 0xFFFF, data & 0xFF);
                if (target.Increment)
                {
                    cpu.Registers.HL++;
                }
                else if (target.Decrement)
                {
                    cpu.Registers.HL--;
                }
                break;
            case "SP":
                cpu.Memory.Write(cpu.Registers.SP & 0xFFFF, data & 0xFF);
                if (target.Increment)
                {
                    cpu.Registers.SP++;
                }
                else if (target.Decrement)
                {
                    cpu.Registers.SP--;
                }
                break;
            case "C":
                cpu.Memory.Write(0xFF00 + (cpu.Memory.Read(cpu.Registers.C) & 0xFF), data & 0xFF);
                break;
            case "a8":
                cpu.Memory.Write(0xFF00 + (cpu.Memory.Read(cpu.Registers.PC + 1) & 0xFF), data & 0xFF);
                break;
            case "a16":
                var address = (cpu.Memory.Read(cpu.Registers.PC + 1) & 0xFF) +
                    ((cpu.Memory.Read(cpu.Registers.PC + 2) & 0xFF) << 8);
                cpu.Memory.Write(address, data & 0xFF);
                break;
            default:
                throw new InvalidOperationException($"Invalid target operand '{target.Name}'!");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadSource(Operand source, Cpu cpu)
    {
        if (source.Immediate)
        {
            switch (source.Name)
            {
                case "A":
                    return cpu.Registers.A & 0xFF;
                case "B":
                    return cpu.Registers.B & 0xFF;
                case "C":
                    return cpu.Registers.C & 0xFF;
                case "D":
                    return cpu.Registers.D & 0xFF;
                case "E":
                    return cpu.Registers.E & 0xFF;
                case "H":
                    return cpu.Registers.H & 0xFF;
                case "L":
                    return cpu.Registers.L & 0xFF;
                case "AF":
                    return cpu.Registers.AF & 0xFFFF;
                case "BC":
                    return cpu.Registers.BC & 0xFFFF;
                case "DE":
                    return cpu.Registers.DE & 0xFFFF;
                case "HL":
                    var hl = cpu.Registers.HL & 0xFFFF;
                    if (source.Increment)
                    {
                        cpu.Registers.HL++;
                    }
                    else if (source.Decrement)
                    {
                        cpu.Registers.HL--;
                    }
                    return hl;
                case "SP":
                    var sp = cpu.Registers.SP & 0xFFFF;
                    if (source.Increment)
                    {
                        cpu.Registers.SP++;
                    }
                    else if (source.Decrement)
                    {
                        cpu.Registers.SP--;
                    }
                    return sp;
                case "d8":
                case "r8":
                    return cpu.Memory.Read(((cpu.Registers.PC + 1) & 0xFFFF)) & 0xFF;
                case "a16":
                case "d16":
                    var lsb = cpu.Memory.Read(((cpu.Registers.PC + 1) & 0xFFFF)) & 0xFF;
                    var hsb = cpu.Memory.Read(((cpu.Registers.PC + 2) & 0xFFFF)) & 0xFF;
                    return (hsb << 8) + lsb;
                default:
                    throw new InvalidOperationException($"Invalid source operand '{source.Name}'!");
            }
        }

        // Indirect
        switch (source.Name)
        {
            case "BC":
                return cpu.Memory.Read(cpu.Registers.BC & 0xFFFF) & 0xFF;
            case "DE":
                return cpu.Memory.Read(cpu.Registers.DE & 0xFFFF) & 0xFF;
            case "HL":
                var hl = cpu.Memory.Read(cpu.Registers.HL & 0xFFFF) & 0xFF;
                if (source.Increment)
                {
                    cpu.Registers.HL++;
                }
                else if (source.Decrement)
                {
                    cpu.Registers.HL--;
                }
                return hl;
            case "C":
                return cpu.Memory.Read(0xFF00 + (cpu.Memory.Read(cpu.Registers.C & 0xFF) & 0xFF)) & 0xFF;
            case "a8":
                return cpu.Memory.Read(0xFF00 + (cpu.Memory.Read((cpu.Registers.PC + 1) & 0xFFFF)) & 0xFF) & 0xFF;
            case "a16":
                var address = cpu.Memory.Read(((cpu.Registers.PC + 1) & 0xFFFF)) & 0xFF +
                    (cpu.Memory.Read(((cpu.Registers.PC + 2) & 0xFFFF)) << 8) & 0xFF00;
                return cpu.Memory.Read(address) & 0xFF;
            default:
                throw new InvalidOperationException($"Invalid source operand '{source.Name}'!");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateFlags(Cpu cpu, bool? zero = null, bool? subtraction = null, bool? halfCarry = null, bool? carry = null)
    {
        if (zero.HasValue)
        {
            cpu.Registers.Zero = zero.Value;
        }

        if (subtraction.HasValue)
        {
            cpu.Registers.Subtraction = subtraction.Value;
        }

        if (halfCarry.HasValue)
        {
            cpu.Registers.HalfCarry = halfCarry.Value;
        }

        if (carry.HasValue)
        {
            cpu.Registers.Carry = carry.Value;
        }
    }
}