namespace GB.Core.Memory
{
    internal interface IRegister
    {
        int Address { get; }
        RegisterType Type { get; }
    }
}
