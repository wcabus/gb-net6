namespace GB.Core.Cpu
{
    internal enum CpuState
    {
        Halted,
        Stopped,
        OpCode,
        ExtendedOpcode,
        Operands,
        Running,
        IRQ_ReadInterruptFlag,
        IRQ_ReadInterruptEnabled,
        IRQ_PushMSB,
        IRQ_PushLSB,
        IRQ_Jump
    }
}
