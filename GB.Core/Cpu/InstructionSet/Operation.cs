using GB.Core.Graphics;

namespace GB.Core.Cpu.InstructionSet
{
    internal abstract class Operation
    {
        public virtual bool ReadsMemory() => false;
        public virtual bool WritesMemory() => false;
        public virtual int Length() => 0;
        public virtual int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context) => context;
        public virtual bool ShouldProceed(CpuRegisters registers) => true;
        public virtual bool ForceFinishCycle() => false;
        public virtual void SwitchInterrupts(InterruptManager interruptManager) { }
        public virtual CorruptionType? CausesOamBug(CpuRegisters registers, int context) => null;

        public static bool InOamArea(int address) => address is >= 0xFE00 and <= 0xFEFF;
    }
}
