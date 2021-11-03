using GB.Core.Controller;
using GB.Core.Gui;
using Button = GB.Core.Controller.Button;
using GB.WinForms.OsSpecific;
using System.Timers;

namespace GB.WinForms
{
    public partial class MainForm : Form, IController
    {
        private IButtonListener? _listener;

        private readonly Dictionary<Keys, Button> _controls;
        private CancellationTokenSource _cancellation;

        private BitmapDisplay _display = new();
        private Emulator _emulator;

        private readonly object _updateLock = new object();

        private uint _frames = 0;

        private System.Timers.Timer _fpsTimer = new System.Timers.Timer(1000);

        public MainForm()
        {
            _emulator = new Emulator
            {
                Display = _display,
                SoundOutput = new SoundOutput()
            };

            InitializeComponent();

            _pictureBox.Top = menuStrip1.Height;
            _pictureBox.Width = BitmapDisplay.DisplayWidth * 5;
            _pictureBox.Height = BitmapDisplay.DisplayHeight * 5;
            _pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            _controls = new Dictionary<Keys, Button>
            {
                {Keys.A, Button.Left},
                {Keys.D, Button.Right},
                {Keys.W, Button.Up},
                {Keys.S, Button.Down},
                {Keys.NumPad4, Button.A},
                {Keys.NumPad8, Button.B},
                {Keys.Enter, Button.Start},
                {Keys.Back, Button.Select}
            };

            Height = menuStrip1.Height + _pictureBox.Height + 50;
            Width = _pictureBox.Width;

            _cancellation = new CancellationTokenSource();

            _emulator.Controller = this;
            _emulator.Display.OnFrameProduced += UpdateDisplay;

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Closed += (_, e) => { _cancellation.Cancel(); };

            _fpsTimer.AutoReset = true;
            _fpsTimer.Elapsed += OnFpsTimerElapsed;
        }

        public void SetButtonListener(IButtonListener listener) => _listener = listener;

        private void UpdateDisplay(object _, EventArgs __)
        {
            if (Monitor.TryEnter(_updateLock))
            {
                try
                {
                    _pictureBox.Image = Image.FromStream(_display.GetFrame());
                    Interlocked.Increment(ref _frames);
                }
                finally
                {
                    Monitor.Exit(_updateLock);
                }
            }
        }

        private void OnFpsTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            var frames = Interlocked.Exchange(ref _frames, 0);
            _fpsLabel.Text = "FPS: " + frames.ToString();
        }

        private void OpenRom(object sender, EventArgs e)
        {
            if (_emulator.Active)
            {
                _fpsTimer.Stop();
                _fpsLabel.Text = "FPS: ";

                _emulator.Stop(_cancellation);
                _cancellation = new CancellationTokenSource();
                _pictureBox.Image = null;
                Thread.Sleep(100);
            }

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
                _fpsTimer.Start();
            }
        }

        private void TogglePause(object sender, EventArgs e)
        {
            _emulator.TogglePause();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var button = _controls.ContainsKey(e.KeyCode) ? _controls[e.KeyCode] : null;
            if (button != null)
            {
                _listener?.OnButtonPress(button);
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
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
            if (_pictureBox == null) return;

            _pictureBox.Width = Width;
            _pictureBox.Height = Height - menuStrip1.Height - 50;
        }
    }
}