﻿using GB.Core.Memory.Cartridge.Battery;
using GB.Core.Memory.Cartridge.Type;
using System.Text;

namespace GB.Core.Memory.Cartridge
{
    public class Cartridge : IAddressSpace
    {
        private int[] _romData = new int[0];
        private IAddressSpace? _addressSpace;

        private int _dmgBootstrap = 0; // use boot rom

        private string _title = "";
        private string _licensee = "";        
        
        private Cartridge() { }

        public static Cartridge? FromFile(string path)
        {
            if (!File.Exists(path)) 
            {
                return null; 
            }

            using var stream = File.OpenRead(path);
            var cartridge = new Cartridge();
            cartridge.Initialize(stream);

            return cartridge;
        }

        private void Initialize(FileStream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            _romData = ms.ToArray().Select(x => (int)x).ToArray();

            var type = CartridgeTypeExtensions.GetById(_romData[0x0147]);
            var gameboyType = GameboyType;
            var romBanks = GetRomBanks(_romData[0x0148]);
            var ramBanks = GetRamBanks(_romData[0x0149]);

            if (ramBanks == 0 && type.IsRam())
            {
                ramBanks = 1;
            }

            // todo battery support
            var battery = new NullBattery();

            if (type.IsMbc1())
            {
                _addressSpace = new Mbc1(_romData, type, battery, romBanks, ramBanks);
            }
            else if (type.IsMbc2())
            {
                _addressSpace = new Mbc2(_romData, type, battery, romBanks);
            }
            else if (type.IsMbc3())
            {
                _addressSpace = new Mbc3(_romData, type, battery, romBanks, ramBanks);
            }
            else if (type.IsMbc5())
            {
                _addressSpace = new Mbc5(_romData, type, battery, romBanks, ramBanks);
            }
            else
            {
                _addressSpace = new Rom(_romData, type, romBanks, ramBanks);
            }

            switch (gameboyType)
            {
                case GameboyType.Standard:
                    IsGameboyColor = false;
                    break;
                case GameboyType.GameboyColor:
                    IsGameboyColor = true;
                    break;
                default: // universal
                    IsGameboyColor = true; // could potentially be overwritten by a global setting
                    break;
            }
        }

        public bool Accepts(int address) => _addressSpace!.Accepts(address) || address == 0xFF50;

        public void SetByte(int address, int value)
        {
            if (address == 0xFF50)
            {
                _dmgBootstrap = 1;
            }
            else
            {
                _addressSpace!.SetByte(address, value);
            }
        }

        public int GetByte(int address)
        {
            switch (_dmgBootstrap)
            {
                case 0 when !IsGameboyColor && (address >= 0x0000 && address < 0x0100):
                    return BootRom.DMG[address];
                case 0 when IsGameboyColor && address >= 0x000 && address < 0x0100:
                    return BootRom.GBC[address];
                case 0 when IsGameboyColor && address >= 0x200 && address < 0x0900:
                    return BootRom.GBC[address - 0x0100];
            }

            return address == 0xFF50 ? 0xFF : _addressSpace!.GetByte(address);
        }

        public bool IsGameboyColor { get; private set; }

        public string Title
        {
            get
            {
                if (_title == "")
                {
                    var sb = new StringBuilder();
                    for (var i = 0x0134; i < 0x0143; i++)
                    {
                        var c = (char)_romData[i];
                        if (c == 0)
                        {
                            break;
                        }

                        sb.Append(c);
                    }
                    _title = sb.ToString();
                }

                return _title;
            }
        }

        public GameboyType GameboyType
        {
            get
            {
                var data = _romData[0x0143];
                return data switch
                {
                    0x80 => GameboyType.Universal,
                    0xC0 => GameboyType.GameboyColor,
                    _ => GameboyType.Standard
                };
            }
        }

        public string Licensee
        {
            get
            {
                if (_licensee == "")
                {
                    // read title
                }

                return _licensee;
            }
        }

        private static int GetRomBanks(int id)
        {
            return id switch
            {
                0 => 2,
                1 => 4,
                2 => 8,
                3 => 16,
                4 => 32,
                5 => 64,
                6 => 128,
                7 => 256,
                0x52 => 72,
                0x53 => 80,
                0x54 => 96,
                _ => throw new ArgumentException($"Unsupported ROM size: 0x{id:X}")
            };
        }

        private static int GetRamBanks(int id)
        {
            return id switch
            {
                0 => 0,
                1 => 1,
                2 => 1,
                3 => 4,
                4 => 16,
                _ => throw new ArgumentException($"Unsupported RAM size: 0x{id:X}")
            };
        }
    }
}