using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace GB.Core.InstructionSet;

internal class OpCode
{
    private readonly int _opCode;
    private readonly string _mnemonic;
    private readonly int _length;
    private readonly int _ticks;
    private readonly int? _ticksWhenActionNotTaken;
    private readonly IReadOnlyCollection<Operand> _operands;
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

    private OpCode(int opCode, string mnemonic, int length, int ticks, IReadOnlyCollection<Operand> operands, bool immediate, InstructionFlags flags) 
        : this(opCode, mnemonic, length, ticks, null, operands, immediate, flags)
    {
    }

    private OpCode(int opCode, string mnemonic, int length, int ticks, int? ticksWhenActionNotTaken, IReadOnlyCollection<Operand> operands, bool immediate, InstructionFlags flags)
    {
        _opCode = opCode;
        _mnemonic = mnemonic;
        _length = length;
        _ticks = ticks;
        _ticksWhenActionNotTaken = ticksWhenActionNotTaken;
        _operands = operands;
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
    public static OpCode Create(int opCode)
    {
        return OpCodes[opCode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Ticks() 
    {
        var cycles = _actionTaken ? _ticks : _ticksWhenActionNotTaken.GetValueOrDefault(_ticks);
        cycles -= (_reduceCyclesByFour ? 4 : 0);
        _reduceCyclesByFour = false;

        return cycles < 0 ? 0 : cycles;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Bytes() => _length;

    public void Execute(Cpu cpu)
    {
        var length = _length;

        if (_opCode == 0xCB)
        {
            cpu.Prefix = 0xCB;
        }
        else
        {
#if DEBUG
            Console.WriteLine(ToString());
#endif
            if (cpu.Prefix == 0xCB)
            {
                length--;
                _reduceCyclesByFour = true;
            }
            cpu.Prefix = 0x00;
        }

        cpu.Registers.PC += length; // could be different when jumping, this is the default for just increasing the PC!
    }
}