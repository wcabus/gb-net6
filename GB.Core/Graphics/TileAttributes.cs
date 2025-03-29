﻿using System.Runtime.CompilerServices;

namespace GB.Core.Graphics
{
    internal sealed class TileAttributes
    {
        public static TileAttributes Empty { get; }
        private static readonly TileAttributes[] Attributes;
        private readonly int _value;

        static TileAttributes()
        {
            Attributes = new TileAttributes[256];

            for (var i = 0; i < 256; i++)
            {
                Attributes[i] = new TileAttributes(i);
            }

            Empty = Attributes[0];
        }

        private TileAttributes(int value) => _value = value;
        
        public static TileAttributes ValueOf(int value) => Attributes[value];
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPriority() => (_value & (1 << 7)) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsYFlip() => (_value & (1 << 6)) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsXFlip() => (_value & (1 << 5)) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GpuRegister GetDmgPalette() => (_value & (1 << 4)) == 0 ? GpuRegister.Obp0 : GpuRegister.Obp1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetBank() => (_value & (1 << 3)) == 0 ? 0 : 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetColorPaletteIndex() => _value & 0x07;
    }
}
