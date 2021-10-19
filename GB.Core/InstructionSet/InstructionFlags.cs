namespace GB.Core.InstructionSet
{
    internal record InstructionFlags(InstructionFlag Z, InstructionFlag N, InstructionFlag H, InstructionFlag C);

    internal enum InstructionFlag
    {
        Unchanged = -1,
        Zero = 0,
        One = 1,
        Determine = 2
    }
}
