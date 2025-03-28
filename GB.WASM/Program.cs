using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using GB.Core.Controller;
using GB.Core.Graphics;
using GB.Core.Gui;
using GB.Core.Sound;
using GB.WASM;

Interop.SynchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();

Console.WriteLine("Loading...");
var gb = new WebGame();
Interop.Game = gb;

Console.WriteLine("Starting the emulator...");
gb.StartGame();

public class WebGame : IController, IDisposable
{
    private IButtonListener? _listener;
    private readonly WebDisplay _display = new();

    // private readonly ISoundOutput _soundOutput = new SoundOutput();
    private readonly ISoundOutput _soundOutput = new NullSoundOutput();
    private readonly Emulator _emulator;
    private readonly Dictionary<string, Button> _controls;

    private CancellationTokenSource _cancellation;

    public WebGame()
    {
        _emulator = new Emulator
        {
            Display = _display,
            SoundOutput = _soundOutput
        };

        _controls = new Dictionary<string, Button>
        {
            {"KeyA", Button.Left},
            {"KeyD", Button.Right},
            {"KeyW", Button.Up},
            {"KeyS", Button.Down},
            {"KeyK", Button.A},
            {"KeyO", Button.B},
            {"Enter", Button.Start},
            {"Backspace", Button.Select}
        };

        _cancellation = new CancellationTokenSource();

        _emulator.Controller = this;
    }

    public void StartGame()
    {
        StopEmulator();

        _emulator.EnableBootRom = true;
        _emulator.RomStream = new MemoryStream(TestResources.SUPERMARIOLAND);
        _emulator.Run(_cancellation.Token);
    }

    public void StopEmulator()
    {
        if (!_emulator.Active)
        {
            return;
        }

        _emulator.Stop(_cancellation);
        _soundOutput.Stop();

        _cancellation = new CancellationTokenSource();
        _display.Enabled = false;
    }

    public void OnKeyDown(string keyCode)
    {
        var button = _controls.GetValueOrDefault(keyCode);
        if (button != null)
        {
            _listener?.OnButtonPress(button);
        }
    }

    public void OnKeyUp(string keyCode)
    {
        var button = _controls.GetValueOrDefault(keyCode);
        if (button != null)
        {
            _listener?.OnButtonRelease(button);
        }
    }

    void IController.SetButtonListener(IButtonListener listener)
    {
        _listener = listener;
    }

    public void Dispose()
    {
        _cancellation?.Dispose();
    }
}

public class WebDisplay : IDisplay
{
    public static readonly int DisplayWidth = 160;
    public static readonly int DisplayHeight = 144;
    public static readonly float AspectRatio = DisplayWidth / (DisplayHeight * 1f);

    public static readonly int[] Colors = { 0xe6f8da, 0x99c886, 0x437969, 0x051f2a };
    private static readonly byte[] ScreenOffOutput;

    private readonly int[] _rgb = new int[DisplayWidth * DisplayHeight];
    private readonly byte[] _imageBuffer = new byte[DisplayWidth * DisplayHeight * 4];

    private bool _doStop;
    private int _doRefresh;
    private int _i;

    static WebDisplay()
    {
        var (r, g, b) = ToRgb(Colors[0]);
        var pi = 0;
        var count = DisplayHeight * DisplayWidth;

        ScreenOffOutput = new byte[count * 4];
        
        while (pi < count)
        {
            ScreenOffOutput[pi++] = (byte)r;
            ScreenOffOutput[pi++] = (byte)g;
            ScreenOffOutput[pi++] = (byte)b;
            ScreenOffOutput[pi++] = 255;
        }
    }

    public void Run(CancellationToken token)
    {
        _doStop = false;
        _doRefresh = 0;
        Enabled = true;

        while (!_doStop)
        {
            Thread.Sleep(1);

            if (Interlocked.And(ref _doRefresh, 1) == 1)
            {
                FillAndDrawBuffer();

                _i = 0;
                Interlocked.Exchange(ref _doRefresh, 0);
            }

            _doStop = token.IsCancellationRequested;
        }
    }

    public bool Enabled { get; set; }

    public void PutDmgPixel(int color)
    {
        _rgb[_i++] = Colors[color];
        _i %= _rgb.Length;
    }

    public void PutColorPixel(int gbcRgb)
    {
        if (_i >= _rgb.Length)
        {
            return;
        }
        _rgb[_i++] = TranslateGbcRgb(gbcRgb);
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
        Interlocked.Exchange(ref _doRefresh, 1);
    }

    public void WaitForRefresh()
    {
        while (Interlocked.And(ref _doRefresh, 1) == 1)
        {
            Thread.Sleep(1);
        }
    }

    private void FillAndDrawBuffer()
    {
        var pi = 0;
        var i = 0;
        while (i < _rgb.Length)
        {
            var (r, g, b) = ToRgb(_rgb[i++]);
            _imageBuffer[pi++] = (byte)r;
            _imageBuffer[pi++] = (byte)g;
            _imageBuffer[pi++] = (byte)b;
            _imageBuffer[pi++] = 255;
        }

        OutputImage();
    }

    private void OutputImage()
    {
        var output = ScreenOffOutput;
        
        if (Enabled)
        {
            output = _imageBuffer;
        }

        // Interop.OutputImage(output, DisplayWidth, DisplayHeight);
        Interop.SynchronizationContext.Post(x => Interop.OutputImage((byte[])x, DisplayWidth, DisplayHeight), output);
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

public partial class Interop
{
    [JSImport("outputImage", "main.js")]
    internal static partial void OutputImage(byte[] pixels, int width, int height);

    [JSExport]
    internal static void KeyDown(string keyCode)
    {
        Game.OnKeyDown(keyCode);
    }

    [JSExport]
    internal static void KeyUp(string keyCode)
    {
        Game.OnKeyUp(keyCode);
    }

    public static SynchronizationContext SynchronizationContext { get; set; }
    public static WebGame Game { get; set; }
}