namespace GB.Core.InstructionSet
{
    internal record Operand(string Name, bool Immediate, int? Bytes, bool Increment, bool Decrement);
}
