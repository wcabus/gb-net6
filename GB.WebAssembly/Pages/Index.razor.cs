using System.Runtime.CompilerServices;
using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;
using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.Core;
using GB.Core.Controller;
using GB.Core.Graphics;
using GB.Core.Gui;
using GB.Core.Sound;
using GB.WebAssembly.ViewModels;
using GB.WebAssembly.Workers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GB.WebAssembly.Pages
{
    public partial class Index : IController, IDisplay
    {
        public static readonly int DisplayWidth = 160;
        public static readonly int DisplayHeight = 144;
        public static readonly int[] Colors = { 0xe6f8da, 0x99c886, 0x437969, 0x051f2a };

        private ElementReference _renderTarget;
        private BECanvasComponent? _canvas = null;
        private Canvas2DContext? _canvasContext;
        private IButtonListener? _listener;

        private readonly ISoundOutput _soundOutput = new NullSoundOutput();
        private readonly Dictionary<string, Button> _controls;
        private readonly Emulator _emulator;

        private readonly Image<Rgba32> _imageBuffer = new(DisplayWidth, DisplayHeight);
        private readonly int[] _rgb;
        private int _rgbIndex;

        private bool _doStop;

        private static bool _waitForRefresh;

        private CancellationTokenSource _cancellation = new();

        private bool _requestedScreenRefresh = false;
        private bool _lcdDisabled = false;
        private readonly MemoryStream _imageStream = new();
        
        private string _imageBytes = "";
        private bool _imageBytesSet = false;
        private float _lastTimeStamp;

        private IWorker? _worker = null;

        public Index()
        {
            _waitForRefresh = false;
            _rgb = new int[DisplayWidth * DisplayHeight];

            _emulator = new Emulator
            {
                Display = this,
                SoundOutput = _soundOutput,
                Controller = this
            };

            _controls = new Dictionary<string, Button>
            {
                {"a", Button.Left},
                {"d", Button.Right},
                {"w", Button.Up},
                {"s", Button.Down},
                {"k", Button.A},
                {"o", Button.B},
                {"\r", Button.Start},
                {"\b", Button.Select}
            };
        }

        [Inject]
        public IJSRuntime? JsRuntime { get; set; }

        public IndexViewModel ViewModel { get; set; } = new();

        private string ImageBytes
        {
            get => _imageBytes;
            set
            {
                _imageBytes = value;
                _imageBytesSet = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            _canvasContext = await _canvas.CreateCanvas2DAsync();
            await JsRuntime!.InvokeAsync<object>("initEmulator", DotNetObjectReference.Create(this));
        }

        [JSInvokable]
        public async ValueTask GameLoop(float timeStamp)
        {
            if (!ViewModel.IsRunning || !_emulator.Active || _emulator.Gameboy?.Paused == true)
            {
                return;
            }

            await DrawEmulator(timeStamp);
        }
        
        private async Task DrawEmulator(float timeStamp)
        {
            if (_canvasContext is null)
            {
                return;
            }

            await _canvasContext!.BeginBatchAsync();

            try
            {
                await _canvasContext.ClearRectAsync(0, 0, 300, 400);

                if (DisplayEnabled && _imageBytesSet && !_waitForRefresh)
                {
                    var fps = 1.0 / (timeStamp - _lastTimeStamp) * 1000;
                    _lastTimeStamp = timeStamp;

                    await _canvasContext.DrawImageAsync(_renderTarget, 0, 0, 320, 288);

                    await _canvasContext.SetFillStyleAsync("#000000");
                    await _canvasContext.SetFontAsync("16px consolas");
                    await _canvasContext.FillTextAsync($"FPS: {fps:0.000}", 10, 10);
                }
                else
                {
                    await _canvasContext.SetFillStyleAsync("#e6f8da");
                    await _canvasContext.FillRectAsync(0, 0, 320, 288);
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {

            }
            finally
            {
                await _canvasContext.EndBatchAsync();
            }
        }

        public void SetButtonListener(IButtonListener listener) => _listener = listener;

        [JSInvokable]
        public void OnKeyDown(string key)
        {
            var button = _controls.ContainsKey(key) ? _controls[key] : null;
            if (button != null)
            {
                _listener?.OnButtonPress(button);
            }
        }

        [JSInvokable]
        public void OnKeyUp(string key)
        {
            var button = _controls.ContainsKey(key) ? _controls[key] : null;
            if (button != null)
            {
                _listener?.OnButtonRelease(button);
            }
        }

        private async Task LoadRom(InputFileChangeEventArgs e)
        {
            ViewModel.IsLoading = true;

            try
            {
                _imageBytes = "";
                _imageBytesSet = false;

                _emulator.SetRomStream(e.File.Name, e.File.OpenReadStream());

                if (_worker != null)
                {
                    await _worker.DisposeAsync();
                    _worker = null;
                }

                _worker = await workerFactory.CreateAsync();
                
                var emulatorRunner = await _worker.CreateBackgroundServiceAsync<EmulatorRunner>(x =>
                    x.AddConventionalAssemblyOfService()
                        .AddAssemblyOf<GB.Core.Gui.Emulator>());
                var localEmulator = _emulator;
                var token = _cancellation.Token;
                await emulatorRunner.RunAsync(s =>  s.Run(localEmulator, token));

                ViewModel.IsRunning = true;

                await _emulator.Display.Run(_cancellation.Token);
            }
            finally
            {
                ViewModel.IsLoading = false;
            }
        }

        private async Task StopEmulator()
        {
            ViewModel.IsRunning = false;
            if (!_emulator.Active)
            {
                return;
            }

            if (_worker is not null)
            {
                await _worker.DisposeAsync();
                _worker = null;
            }

            _emulator.Stop(_cancellation);
            _soundOutput.Stop();

            _cancellation = new CancellationTokenSource();
            DisplayEnabled = false;
            await Task.Delay(100);
        }
        
        public bool DisplayEnabled { get; set; }

        bool IDisplay.Enabled
        {
            get => DisplayEnabled;
            set => DisplayEnabled = value;
        }

        public void PutDmgPixel(int color)
        {
            _rgb[_rgbIndex++] = Colors[color];
            _rgbIndex %= _rgb.Length;
        }

        public void PutColorPixel(int gbcRgb)
        {
            if (_rgbIndex >= _rgb.Length)
            {
                return;
            }
            _rgb[_rgbIndex++] = TranslateGbcRgb(gbcRgb);
        }

        public static int TranslateGbcRgb(int gbcRgb)
        {
            var r = (gbcRgb >> 0) & 0x1f;
            var g = (gbcRgb >> 5) & 0x1f;
            var b = (gbcRgb >> 10) & 0x1f;
            var result = (r * 8) << 16;
            result |= (g * 8) << 8;
            result |= (b * 8) << 0;
            return result;
        }

        public void RequestRefresh()
        {
            FillAndDrawBuffer();

            _rgbIndex = 0;
            _waitForRefresh = false;

            _doStop = _cancellation.Token.IsCancellationRequested;
        }

        public void WaitForRefresh()
        {
            _waitForRefresh = true;
            //while (_doRefresh)
            //{
            //    try
            //    {
            //        Task.Delay(1).RunSynchronously();
            //    }
            //    catch (TaskCanceledException)
            //    {
            //        break;
            //    }
            //}
        }

        Task IRunnable.Run(CancellationToken token)
        {
            _doStop = false;
            _waitForRefresh = false;
            DisplayEnabled = true;

            //while (!_doStop)
            //{
            //    try
            //    {
            //        Task.Delay(1, token).RunSynchronously();
            //    }
            //    catch (TaskCanceledException)
            //    {
            //        break;
            //    }

            //    if (_doRefresh)
            //    {
            //        FillAndDrawBuffer();

            //        _rgbIndex = 0;
            //        _doRefresh = false;
            //    }

            //    _emulator?.Gameboy?.RunOnce(ref requestedScreenRefresh, ref lcdDisabled);

            //    _doStop = token.IsCancellationRequested;
            //}

            return Task.CompletedTask;
        }

        public bool HasGameloop => true;

        private void FillAndDrawBuffer()
        {
            try
            {
                var pi = 0;
                while (pi < _rgb.Length)
                {
                    var (r, g, b) = ToRgb(_rgb[pi]);
                    _imageBuffer[pi % DisplayWidth, pi++ / DisplayWidth] = new Rgba32((byte)r, (byte)g, (byte)b, 255);
                }

                _imageStream.Seek(0, SeekOrigin.Begin);
                _imageBuffer.SaveAsPng(_imageStream);
                ImageBytes = "data:image/png;base64," + Convert.ToBase64String(_imageStream.ToArray());
            }
            catch (ObjectDisposedException) { }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int, int) ToRgb(int pixel)
        {
            var b = pixel & 255;
            var g = (pixel >> 8) & 255;
            var r = (pixel >> 16) & 255;
            return (r, g, b);
        }
    }
}