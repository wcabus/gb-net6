using System.Runtime.CompilerServices;

namespace GB.Core.InstructionSet;

internal class OpCode
{
    private readonly int _opCode;
    private readonly int _length;
    private readonly int _ticks;
    private readonly int _ticksWhenActionNotTaken;

    private bool _actionTaken = true;

    private static readonly Dictionary<int, OpCode> OpCodes = new();

    static OpCode()
    {
        InitializeOpCodes();
    }

    private static void InitializeOpCodes()
    {
        OpCodes.Add(0x00, new OpCode(0x00, 1, 4)); // NOP
        OpCodes.Add(0x10, new OpCode(0x10, 2, 4)); // STOP d8
        OpCodes.Add(0x20, new OpCode(0x20, 2, 12, 8)); // JR NZ, r8
        OpCodes.Add(0x30, new OpCode(0x30, 2, 12, 8)); // JR NC, r8
        OpCodes.Add(0x40, new OpCode(0x40, 1, 4)); // LD B, B
        OpCodes.Add(0x50, new OpCode(0x50, 1, 4)); // LD D, B
        OpCodes.Add(0x60, new OpCode(0x60, 1, 4)); // LD H, B
        OpCodes.Add(0x70, new OpCode(0x70, 1, 8)); // LD (HL), B
        OpCodes.Add(0x80, new OpCode(0x80, 1, 4)); // ADD A, B
        OpCodes.Add(0x90, new OpCode(0x90, 1, 4)); // SUB B
        OpCodes.Add(0xA0, new OpCode(0xA0, 1, 4)); // AND B
        OpCodes.Add(0xB0, new OpCode(0xB0, 1, 4)); // OR B
        OpCodes.Add(0xC0, new OpCode(0xC0, 1, 20, 8)); // RET NZ
        OpCodes.Add(0xD0, new OpCode(0xD0, 1, 20, 8)); // RET NC
        OpCodes.Add(0xE0, new OpCode(0xE0, 2, 12)); // LDH (a8), A
        OpCodes.Add(0xF0, new OpCode(0xF0, 2, 12)); // LDH A, (a8)

        OpCodes.Add(0x01, new OpCode(0x01, 3, 12)); // LD BC, d16
        OpCodes.Add(0x11, new OpCode(0x11, 3, 12)); // LD DE, d16
        OpCodes.Add(0x21, new OpCode(0x21, 3, 12)); // LD HL, d16
        OpCodes.Add(0x31, new OpCode(0x31, 3, 12)); // LD SP, d16
        OpCodes.Add(0x41, new OpCode(0x41, 1, 4)); // LD B, C
        OpCodes.Add(0x51, new OpCode(0x51, 1, 4)); // LD D, C
        OpCodes.Add(0x61, new OpCode(0x61, 1, 4)); // LD H, C
        OpCodes.Add(0x71, new OpCode(0x71, 1, 8)); // LD (HL), C
        OpCodes.Add(0x81, new OpCode(0x81, 1, 4)); // ADD A, C
        OpCodes.Add(0x91, new OpCode(0x91, 1, 4)); // SUB C
        OpCodes.Add(0xA1, new OpCode(0xA1, 1, 4)); // AND C
        OpCodes.Add(0xB1, new OpCode(0xB1, 1, 4)); // OR C
        OpCodes.Add(0xC1, new OpCode(0xC1, 1, 12)); // POP BC
        OpCodes.Add(0xD1, new OpCode(0xD1, 1, 12)); // POP DE
        OpCodes.Add(0xE1, new OpCode(0xE1, 1, 12)); // POP HL
        OpCodes.Add(0xF1, new OpCode(0xF1, 1, 12)); // POP AF

        OpCodes.Add(0x02, new OpCode(0x02, 1, 8)); // LD (BC), A
        OpCodes.Add(0x12, new OpCode(0x12, 1, 8)); // LD (DE), A
        OpCodes.Add(0x22, new OpCode(0x22, 1, 8)); // LD (HL+), A
        OpCodes.Add(0x32, new OpCode(0x32, 1, 8)); // LD (HL-), A
        OpCodes.Add(0x42, new OpCode(0x42, 1, 4)); // LD B, D
        OpCodes.Add(0x52, new OpCode(0x52, 1, 4)); // LD D, D
        OpCodes.Add(0x62, new OpCode(0x62, 1, 4)); // LD H, D
        OpCodes.Add(0x72, new OpCode(0x72, 1, 8)); // LD (HL), D
        OpCodes.Add(0x82, new OpCode(0x82, 1, 4)); // ADD A, D
        OpCodes.Add(0x92, new OpCode(0x92, 1, 4)); // SUB D
        OpCodes.Add(0xA2, new OpCode(0xA2, 1, 4)); // AND D
        OpCodes.Add(0xB2, new OpCode(0xB2, 1, 4)); // OR D
        OpCodes.Add(0xC2, new OpCode(0xC2, 3, 16, 12)); // JP NZ, a16
        OpCodes.Add(0xD2, new OpCode(0xD2, 3, 16, 12)); // JP NC, a16
        OpCodes.Add(0xE2, new OpCode(0xE2, 1, 8)); // LD (C), A
        OpCodes.Add(0xF2, new OpCode(0xF2, 1, 8)); // LD A, (C)

        OpCodes.Add(0x03, new OpCode(0x03, 1, 8)); // INC BC
        OpCodes.Add(0x13, new OpCode(0x13, 1, 8)); // INC DE
        OpCodes.Add(0x23, new OpCode(0x23, 1, 8)); // INC HL
        OpCodes.Add(0x33, new OpCode(0x33, 1, 8)); // INC SP
        OpCodes.Add(0x43, new OpCode(0x43, 1, 4)); // LD B, E
        OpCodes.Add(0x53, new OpCode(0x53, 1, 4)); // LD D, E
        OpCodes.Add(0x63, new OpCode(0x63, 1, 4)); // LD H, E
        OpCodes.Add(0x73, new OpCode(0x73, 1, 8)); // LD (HL), E
        OpCodes.Add(0x83, new OpCode(0x83, 1, 4)); // ADD A, E
        OpCodes.Add(0x93, new OpCode(0x93, 1, 4)); // SUB E
        OpCodes.Add(0xA3, new OpCode(0xA3, 1, 4)); // AND E
        OpCodes.Add(0xB3, new OpCode(0xB3, 1, 4)); // OR E
        OpCodes.Add(0xC3, new OpCode(0xC3, 3, 16)); // JP a16
        // D3 - not used
        // E3 - not used
        OpCodes.Add(0xF3, new OpCode(0xF3, 1, 4)); // DI

        OpCodes.Add(0x04, new OpCode(0x04, 1, 4)); // INC B
        OpCodes.Add(0x14, new OpCode(0x14, 1, 4)); // INC D
        OpCodes.Add(0x24, new OpCode(0x24, 1, 4)); // INC H
        OpCodes.Add(0x34, new OpCode(0x34, 1, 12)); // INC (HL)
        OpCodes.Add(0x44, new OpCode(0x44, 1, 4)); // LD B, H
        OpCodes.Add(0x54, new OpCode(0x54, 1, 4)); // LD D, H
        OpCodes.Add(0x64, new OpCode(0x64, 1, 4)); // LD H, H
        OpCodes.Add(0x74, new OpCode(0x74, 1, 8)); // LD (HL), H
        OpCodes.Add(0x84, new OpCode(0x84, 1, 4)); // ADD A, H
        OpCodes.Add(0x94, new OpCode(0x94, 1, 4)); // SUB H
        OpCodes.Add(0xA4, new OpCode(0xA4, 1, 4)); // AND H
        OpCodes.Add(0xB4, new OpCode(0xB4, 1, 4)); // OR H
        OpCodes.Add(0xC4, new OpCode(0xC4, 3, 24, 12)); // CALL NZ, a16
        OpCodes.Add(0xD4, new OpCode(0xD4, 3, 24, 12)); // CALL NC, a16
        // E4 - not used
        // F4 - not used

        OpCodes.Add(0x05, new OpCode(0x05, 1, 4)); // DEC B
        OpCodes.Add(0x15, new OpCode(0x15, 1, 4)); // DEC D
        OpCodes.Add(0x25, new OpCode(0x25, 1, 4)); // DEC H
        OpCodes.Add(0x35, new OpCode(0x35, 1, 12)); // DEC (HL)
        OpCodes.Add(0x45, new OpCode(0x45, 1, 4)); // LD B, L
        OpCodes.Add(0x55, new OpCode(0x55, 1, 4)); // LD D, L
        OpCodes.Add(0x65, new OpCode(0x65, 1, 4)); // LD H, L
        OpCodes.Add(0x75, new OpCode(0x75, 1, 8)); // LD (HL), L
        OpCodes.Add(0x85, new OpCode(0x85, 1, 4)); // ADD A, L
        OpCodes.Add(0x95, new OpCode(0x95, 1, 4)); // SUB L
        OpCodes.Add(0xA5, new OpCode(0xA5, 1, 4)); // AND L
        OpCodes.Add(0xB5, new OpCode(0xB5, 1, 4)); // OR L
        OpCodes.Add(0xC5, new OpCode(0xC5, 1, 16)); // PUSH BC
        OpCodes.Add(0xD5, new OpCode(0xD5, 1, 16)); // PUSH DE
        OpCodes.Add(0xE5, new OpCode(0xE5, 1, 16)); // PUSH HL
        OpCodes.Add(0xF5, new OpCode(0xF5, 1, 16)); // PUSH AF

        // https://gbdev.io/gb-opcodes//optables/dark
    }

    private OpCode(int opCode, int length, int ticks) : this(opCode, length, ticks, ticks)
    {
    }

    private OpCode(int opCode, int length, int ticks, int ticksWhenActionNotTaken)
    {
        _opCode = opCode;
        _length = length;
        _ticks = ticks;
        _ticksWhenActionNotTaken = ticksWhenActionNotTaken;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpCode Create(int opCode)
    {
        return OpCodes[opCode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Ticks() => _actionTaken ? _ticks : _ticksWhenActionNotTaken;

    public void Execute(Cpu cpu)
    {

    }
}