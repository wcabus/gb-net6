using GB.Core.Controller;
using GB.Core.Gui;
using Button = GB.Core.Controller.Button;
using GB.WinForms.OsSpecific;
using GB.Core.Sound;

namespace GB.WinForms
{
    public partial class MainForm : Form, IController
    {
        private IButtonListener? _listener;
        
        private readonly Dictionary<Keys, Button> _controls;
        private CancellationTokenSource _cancellation;

        private readonly BitmapDisplay _display = new();
        private readonly ISoundOutput _soundOutput = new SoundOutput();
        private readonly Emulator _emulator;

        public MainForm()
        {
            _emulator = new Emulator
            {
                Display = _display,
                SoundOutput = _soundOutput
            };

            InitializeComponent();

            // _pictureBox
            _display.Top = menuStrip1.Height;
            _display.Width = BitmapDisplay.DisplayWidth * 5;
            _display.Height = BitmapDisplay.DisplayHeight * 5;
            Controls.Add(_display);
            // _display.SizeMode = PictureBoxSizeMode.Zoom;

            _controls = new Dictionary<Keys, Button>
            {
                {Keys.A, Button.Left},
                {Keys.D, Button.Right},
                {Keys.W, Button.Up},
                {Keys.S, Button.Down},
                {Keys.K, Button.A},
                {Keys.O, Button.B},
                {Keys.Enter, Button.Start},
                {Keys.Back, Button.Select}
            };

            _cancellation = new CancellationTokenSource();

            _emulator.Controller = this;
            // _emulator.Display.OnFrameProduced += UpdateDisplay;

            Load += (sender, args) =>
            {
                Height = menuStrip1.Height + _display.Height + 50;
                Width = _display.Width;
            };
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Closed += (_, e) => { _cancellation.Cancel(); };
        }

        public void SetButtonListener(IButtonListener listener) => _listener = listener;

        private void StopEmulator()
        {
            if (!_emulator.Active)
            {
                return;
            }

            _emulator.Stop(_cancellation);
            _soundOutput.Stop();

            _cancellation = new CancellationTokenSource();
            _display.DisplayEnabled = false;
            Thread.Sleep(100);
        }

        private void OpenRom(object sender, EventArgs e)
        {
            StopEmulator();

            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Gameboy ROM (*.gb)|*.gb| All files(*.*) |*.*",
                FilterIndex = 0,
                RestoreDirectory = true
            };

            var (success, romPath) = openFileDialog.ShowDialog() == DialogResult.OK
                ? (true, openFileDialog.FileName)
                : (false, null);

            if (success && !string.IsNullOrEmpty(romPath))
            {
                _emulator.RomPath = romPath;
                _emulator.Run(_cancellation.Token);
            }
        }

        private void TogglePause(object sender, EventArgs e)
        {
            _emulator.TogglePause();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.KeyCode) ? _controls[e.KeyCode] : null;
            if (button != null)
            {
                _listener?.OnButtonPress(button);
            }
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.KeyCode) ? _controls[e.KeyCode] : null;
            if (button != null)
            {
                _listener?.OnButtonRelease(button);
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            _display.Width = Width;
            _display.Height = Height - menuStrip1.Height - 50;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopEmulator();

            base.OnFormClosing(e);
        }
    }
}