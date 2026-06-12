using Godot;
using System.IO;
using System.Threading.Tasks;

// Servicio de desarrollo: vuelca el framebuffer de la ventana principal a un PNG
// en disco para verificación visual (equipamiento, orbes, glows, terreno) sin
// depender de capturas manuales. Dos disparadores:
//   - Auto-captura periódica (Timer) -> sobrescribe latest.png. Es la vía pensada
//     para inspección automatizada: el archivo siempre refleja el estado actual.
//   - Tecla F12 -> latest.png + una copia con timestamp (uso manual).
// La ruta res://.captures/ sólo es escribible corriendo sin empaquetar (editor o
// --path), que es el caso de desarrollo de este monitor.
public partial class ScreenshotService : Node
{
    [Export] public bool   AutoCapture     = true;
    [Export] public float  CaptureInterval = 2.0f;          // segundos entre auto-capturas
    [Export] public string OutputDir       = "res://.captures";

    private string _dirAbs;
    private string _latestPath;
    private bool   _capturing;                              // el readback es asíncrono; evita solapar capturas

    public override void _Ready()
    {
        _dirAbs = ProjectSettings.GlobalizePath(OutputDir);
        Directory.CreateDirectory(_dirAbs);
        _latestPath = Path.Combine(_dirAbs, "latest.png");

        if (AutoCapture)
        {
            var timer = new Timer { WaitTime = CaptureInterval, Autostart = true, OneShot = false };
            timer.Timeout += () => _ = Capture(null);
            AddChild(timer);
        }

        GD.Print($"[ScreenshotService] capturas en: {_dirAbs} (auto={AutoCapture}, F12 manual)");
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.F12)
        {
            var stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _ = Capture(Path.Combine(_dirAbs, $"shot_{stamp}.png"));
        }
    }

    // Guarda latest.png y, si extraPath != null, una copia adicional.
    private async Task Capture(string extraPath)
    {
        if (_capturing) return;
        _capturing = true;
        try
        {
            // Espera a que el frame esté dibujado para leer el render final (post-proceso/glow incluidos).
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var img = GetViewport()?.GetTexture()?.GetImage();
            if (img == null) return;                        // aún sin frame
            img.SavePng(_latestPath);
            if (extraPath != null) img.SavePng(extraPath);
        }
        finally
        {
            _capturing = false;
        }
    }
}
