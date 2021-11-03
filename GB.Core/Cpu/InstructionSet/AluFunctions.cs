namespace GB.Core.Cpu.InstructionSet
{
    /// <summary>
    /// Various arithmetic or logic operations to combine calculation/comparisons and updating the CPU flags
    /// </summary>
    internal static class AluFunctions
    {
        private record FunctionKey(string Name, DataType FirstType, DataType SecondType = DataType.None);

        private static readonly Dictionary<FunctionKey, Func<Flags, int, int>> _functionsOneOperand = new();
        private static readonly Dictionary<FunctionKey, Func<Flags, int, int, int>> _functionsTwoOperands = new();

        static AluFunctions()
        {
            AddFunction("INC", DataType.d8, (flags, arg) =>
            {
                var result = (arg + 1) & 0x00FF;
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH((arg & 0x0F) == 0x0F);
                return result;
            });
            AddFunction("INC", DataType.d16, (flags, arg) => (arg + 1) & 0xFFFF);

            AddFunction("DEC", DataType.d8, (flags, arg) =>
            {
                var result = (arg - 1) & 0x00FF;
                flags.SetZ(result == 0);
                flags.SetN(true);
                flags.SetH((arg & 0x0F) == 0x00);
                return result;
            });
            AddFunction("DEC", DataType.d16, (flags, arg) => (arg - 1) & 0xFFFF);

            AddFunction("ADD", DataType.d8, DataType.d8, (flags, arg1, arg2) =>
            {
                flags.SetZ(((arg1 + arg2) & 0xFF) == 0);
                flags.SetN(false);
                flags.SetH((arg1 & 0x0F) + (arg2 & 0x0F) > 0x0F);
                flags.SetC(arg1 + arg2 > 0xFF);

                return (arg1 + arg2) & 0xFF;
            });
            AddFunction("ADC", DataType.d8, DataType.d8, (flags, arg1, arg2) =>
            {
                var carry = flags.IsC() ? 1 : 0;
                flags.SetZ(((arg1 + arg2 + carry) & 0xff) == 0);
                flags.SetN(false);
                flags.SetH((arg1 & 0x0f) + (arg2 & 0x0f) + carry > 0x0f);
                flags.SetC(arg1 + arg2 + carry > 0xff);
                
                return (arg1 + arg2 + carry) & 0xff;
            });

            AddFunction("SUB", DataType.d8, DataType.d8, (flags, arg1, arg2) =>
            {
                flags.SetZ(((arg1 - arg2) & 0xff) == 0);
                flags.SetN(true);
                flags.SetH((0x0f & arg2) > (0x0f & arg1));
                flags.SetC(arg2 > arg1);

                return (arg1 - arg2) & 0xff;
            });
            AddFunction("SBC", DataType.d8, DataType.d8, (flags, arg1, arg2) =>
            {
                var carry = flags.IsC() ? 1 : 0;
                var res = arg1 - arg2 - carry;

                flags.SetZ((res & 0xff) == 0);
                flags.SetN(true);
                flags.SetH(((arg1 ^ arg2 ^ (res & 0xff)) & (1 << 4)) != 0);
                flags.SetC(res < 0);

                return res & 0xff;
            });

            AddFunction("AND", DataType.d8, DataType.d8, (flags, arg1, arg2) =>
            {
                var result = arg1 & arg2;
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(true);
                flags.SetC(false);

                return result;
            });
            AddFunction("OR", DataType.d8, DataType.d8, (flags, arg1, arg2) =>
            {
                var result = arg1 | arg2;
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                flags.SetC(false);

                return result;
            });
            AddFunction("XOR", DataType.d8, DataType.d8, (flags, arg1, arg2) =>
            {
                var result = (arg1 ^ arg2) & 0xff;
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                flags.SetC(false);

                return result;
            });

            AddFunction("CP", DataType.d8, DataType.d8, (flags, arg1, arg2) =>
            {
                flags.SetZ(((arg1 - arg2) & 0xff) == 0);
                flags.SetN(true);
                flags.SetH((0x0f & arg2) > (0x0f & arg1));
                flags.SetC(arg2 > arg1);

                return arg1;
            });

            AddFunction("RLC", DataType.d8, (flags, arg) =>
            {
                var result = (arg << 1) & 0xff;
                if ((arg & (1 << 7)) != 0)
                {
                    result |= 1;
                    flags.SetC(true);
                }
                else
                {
                    flags.SetC(false);
                }

                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });
            AddFunction("RRC", DataType.d8, (flags, arg) =>
            {
                var result = arg >> 1;
                if ((arg & 1) == 1)
                {
                    result |= (1 << 7);
                    flags.SetC(true);
                }
                else
                {
                    flags.SetC(false);
                }

                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });

            AddFunction("RL", DataType.d8, (flags, arg) =>
            {
                var result = (arg << 1) & 0xff;
                result |= flags.IsC() ? 1 : 0;
                flags.SetC((arg & (1 << 7)) != 0);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });
            AddFunction("RR", DataType.d8, (flags, arg) =>
            {
                var result = arg >> 1;
                result |= flags.IsC() ? (1 << 7) : 0;
                flags.SetC((arg & 1) != 0);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });

            AddFunction("SLA", DataType.d8, (flags, arg) =>
            {
                var result = (arg << 1) & 0xff;
                flags.SetC((arg & (1 << 7)) != 0);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });
            AddFunction("SRA", DataType.d8, (flags, arg) =>
            {
                var result = (arg >> 1) | (arg & (1 << 7));
                flags.SetC((arg & 1) != 0);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });

            AddFunction("SWAP", DataType.d8, (flags, arg) =>
            {
                var upper = arg & 0xf0;
                var lower = arg & 0x0f;
                var result = (lower << 4) | (upper >> 4);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                flags.SetC(false);
                return result;
            });

            AddFunction("SRL", DataType.d8, (flags, arg) =>
            {
                var result = (arg >> 1);
                flags.SetC((arg & 1) != 0);
                flags.SetZ(result == 0);
                flags.SetN(false);
                flags.SetH(false);
                return result;
            });

            AddFunction("ADD", DataType.d16, DataType.d16, (flags, arg1, arg2) =>
            {
                flags.SetN(false);
                flags.SetH((arg1 & 0x0FFF) + (arg2 & 0x0FFF) > 0x0FFF);
                flags.SetC(arg1 + arg2 > 0xFFFF);
                return (arg1 + arg2) & 0xFFFF;
            });
            AddFunction("ADD", DataType.d16, DataType.r8, (flags, arg1, arg2) => (arg1 + arg2) & 0xFFFF);
            AddFunction("ADD_SP", DataType.d16, DataType.r8, (flags, arg1, arg2) =>
            {
                flags.SetZ(false);
                flags.SetN(false);

                flags.SetC((((arg1 & 0xFF) + (arg2 & 0xFF)) & 0x100) != 0);
                flags.SetH((((arg1 & 0x0F) + (arg2 & 0x0F)) & 0x10) != 0);
                return (arg1 + arg2) & 0xFFFF;
            });

            AddFunction("DAA", DataType.d8, (flags, arg) => 
            {
                var result = arg;
                if (flags.IsN())
                {
                    if (flags.IsH())
                    {
                        result = (result - 6) & 0xFF;
                    }
                    if (flags.IsC())
                    {
                        result = (result - 0x60) & 0xFF;
                    }
                }
                else
                {
                    if (flags.IsH() || (result & 0x0F) > 9)
                    {
                        result += 6;
                    }
                    if (flags.IsC() || result > 0x9F)
                    {
                        result += 0x60;
                    }
                }

                flags.SetH(false);
                if (result > 0xFF)
                {
                    flags.SetC(true);
                }

                result &= 0xFF;
                flags.SetZ(result == 0);
                return result;
            });

            AddFunction("CPL", DataType.d8, (flags, arg) => 
            {
                flags.SetN(true);
                flags.SetH(true);
                return (~arg) & 0xFF;
            });

            AddFunction("SCF", DataType.d8, (flags, arg) =>
            {
                flags.SetH(false);
                flags.SetN(false);
                flags.SetC(true);
                return arg;
            });
            AddFunction("CCF", DataType.d8, (flags, arg) =>
            {
                flags.SetH(false);
                flags.SetN(false);
                flags.SetC(!flags.IsC());
                return arg;
            });

            AddFunction("BIT", DataType.d8, DataType.d8, (flags, arg1, arg2) =>
            {
                var bit = arg2;
                flags.SetN(false);
                flags.SetH(true);
                if (bit < 8)
                {
                    flags.SetZ(!arg1.GetBit(arg2));
                }

                return arg1;
            });

            AddFunction("RES", DataType.d8, DataType.d8, (flags, arg1, arg2) => arg1.ClearBit(arg2));
            AddFunction("SET", DataType.d8, DataType.d8, (flags, arg1, arg2) => arg1.SetBit(arg2));
        }

        public static Func<Flags, int, int> GetFunction(string name, DataType dataType) => _functionsOneOperand[new FunctionKey(name, dataType)];
        public static Func<Flags, int, int, int> GetFunction(string name, DataType first, DataType second) => _functionsTwoOperands[new FunctionKey(name, first, second)];

        private static void AddFunction(string name, DataType dataType, Func<Flags, int, int> function) 
        {
            _functionsOneOperand.Add(new FunctionKey(name, dataType), function);
        }

        private static void AddFunction(string name, DataType first, DataType second, Func<Flags, int, int, int> function)
        {
            _functionsTwoOperands.Add(new FunctionKey(name, first, second), function);
        }
    }
}
