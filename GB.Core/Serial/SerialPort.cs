using GB.Core.Cpu;
using System.Diagnostics;

namespace GB.Core.Serial
{
    internal class SerialPort : IAddressSpace
    {
        private readonly ISerialEndpoint _serialEndpoint;
        private readonly InterruptManager _interruptManager;
        private readonly SpeedMode _speedMode;
        private int _sb;
        private int _sc;
        private bool _transferInProgress;
        private int _divider;

        public SerialPort(InterruptManager interruptManager, ISerialEndpoint serialEndpoint, SpeedMode speedMode)
        {
            _interruptManager = interruptManager;
            _serialEndpoint = serialEndpoint;
            _speedMode = speedMode;
        }

        public void Tick()
        {
            if (!_transferInProgress)
            {
                return;
            }

            if (++_divider >= Gameboy.TicksPerSec / 8192 / _speedMode.GetSpeedMode())
            {
                _transferInProgress = false;
                try
                {
                    _sb = _serialEndpoint.Transfer(_sb);
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"Can't transfer byte {e}");
                    _sb = 0;
                }

                _interruptManager.RequestInterrupt(InterruptManager.InterruptType.Serial);
            }
        }

        public bool Accepts(int address)
        {
            return address is 0xFF01 or 0xFF02;
        }

        public void SetByte(int address, int value)
        {
            if (address == 0xFF01)
            {
                _sb = value;
            }
            else if (address == 0xFF02)
            {
                _sc = value;
                if ((_sc & (1 << 7)) != 0)
                {
                    StartTransfer();
                }
            }
        }

        public int GetByte(int address)
        {
            if (address == 0xFF01)
            {
                return _sb;
            } 
            if (address == 0xFF02)
            {
                return _sc | 0b01111110;
            }
            throw new ArgumentException();
        }

        private void StartTransfer()
        {
            _transferInProgress = true;
            _divider = 0;
        }
    }
}
