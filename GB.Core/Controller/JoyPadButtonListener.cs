﻿using GB.Core.Cpu;
using System.Collections.Concurrent;

namespace GB.Core.Controller
{
    internal sealed class JoyPadButtonListener : IButtonListener
    {
        private readonly InterruptManager _interruptManager;
        private readonly ConcurrentDictionary<Button, Button> _buttons;

        public JoyPadButtonListener(InterruptManager interruptManager, ConcurrentDictionary<Button, Button> buttons)
        {
            _interruptManager = interruptManager;
            _buttons = buttons;
        }

        public void OnButtonPress(Button button)
        {
            _interruptManager.RequestInterrupt(InterruptManager.InterruptType.P1013);
            _buttons.TryAdd(button, button);
        }

        public void OnButtonRelease(Button button)
        {
            _buttons.TryRemove(button, out _);
        }
    }
}
