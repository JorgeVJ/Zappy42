using Godot;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Servicio de desarrollo: vuelca el framebuffer de la ventana principal a un PNG en
/// disco para verificación visual (equipamiento, orbes, glows, terreno) sin depender
/// de capturas manuales.
/// </summary>
/// <remarks>
/// Dos disparadores:
///   - Auto-captura periódica (Timer) -&gt; sobrescribe latest.png. Es la vía pensada
///     para inspección automatizada: el archivo siempre refleja el estado actual.
///   - Tecla F12 -&gt; latest.png + una copia con timestamp (uso manual).
/// La ruta res://.captures/ sólo es escribible corriendo sin empaquetar (editor o
/// --path), que es el caso de desarrollo de este monitor.
/// </remarks>
public partial class ScreenshotService : Node
{
    [Export]
    public bool AutoCapture = true;

    /// <summary>Segundos entre auto-capturas.</summary>
    [Export]
    public float CaptureInterval = 2.0f;

    [Export]
    public string OutputDir = "res://.captures";

    private string _dirAbs;
    private string _latestPath;

    /// <summary>El readback es asíncrono; evita solapar capturas.</summary>
    private bool _capturing;

    public override void _Ready()
    {
        _dirAbs = ProjectSettings.GlobalizePath(OutputDir);
        Directory.CreateDirectory(_dirAbs);
        _latestPath = Path.Combine(_dirAbs, "latest.png");

        if (AutoCapture)
        {
            Timer timer = new Timer { WaitTime = CaptureInterval, Autostart = true, OneShot = false };
            timer.Timeout += () => _ = Capture(null);
            AddChild(timer);
        }

        Log.Debug($"[ScreenshotService] capturas en: {_dirAbs} (auto={AutoCapture}, F12 manual)");
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.F12)
        {
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _ = Capture(Path.Combine(_dirAbs, $"shot_{stamp}.png"));
        }
    }

    /// <summary>Guarda latest.png y, si <paramref name="extraPath"/> no es null, una copia adicional.</summary>
    /// <remarks>Espera a que el frame esté dibujado para leer el render final (post-proceso/glow incluidos).</remarks>
    private async Task Capture(string extraPath)
    {
        if (_capturing) return;
        _capturing = true;
        try
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Image img = GetViewport()?.GetTexture()?.GetImage();
            if (img == null) return;
            img.SavePng(_latestPath);
            if (extraPath != null) img.SavePng(extraPath);
        }
        finally
        {
            _capturing = false;
        }
    }
}
