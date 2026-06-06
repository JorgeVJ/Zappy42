using Godot;

public partial class MessageLogPanel : CollapsiblePanel
{
    private RichTextLabel _richText;
    private int _entryCount = 0;
    private const int MaxLines = 80;

    public override void _Ready()
    {
        var vp = GetViewportRect().Size;

        // Panel esquina inferior izquierda: 400x300, margen 10px
        Setup("Mensajes del servidor",
            new Rect2(10, vp.Y - 310, 400, 300),
            new Vector2(10, vp.Y - 42));

        // RichTextLabel ocupa todo el espacio disponible
        _richText = new RichTextLabel();
        _richText.BbcodeEnabled    = true;
        _richText.ScrollFollowing  = true;
        _richText.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        Content.AddChild(_richText);

        // Botón "Limpiar" en la parte inferior derecha del contenido
        var footer = new HBoxContainer();
        footer.Alignment = BoxContainer.AlignmentMode.End;
        var clearBtn = new Button();
        clearBtn.Text     = "Limpiar";
        clearBtn.Pressed += () => { _richText.Clear(); _entryCount = 0; };
        footer.AddChild(clearBtn);
        Content.AddChild(footer);
    }

    public void Log(string command, string rawLine)
    {
        if (_richText == null) return;

        if (_entryCount >= MaxLines)
        {
            _richText.Clear();
            _entryCount = 0;
        }

        string color = GetColor(command);
        string time  = System.DateTime.Now.ToString("HH:mm:ss");
        _richText.AppendText($"[color={color}][{time}] {rawLine.Trim()}[/color]\n");
        _entryCount++;
    }

    private static string GetColor(string cmd) => cmd switch
    {
        "pnw"                              => "#00e5ff", // spawn jugador — cyan
        "pdi"                              => "#ff4444", // muerte jugador — rojo
        "ppo"                              => "#dddddd", // movimiento — blanco suave
        "plv"                              => "#44ff88", // nivel — verde
        "pbc"                              => "#ffee00", // broadcast — amarillo
        "pic" or "pie"                     => "#ff8800", // incantación — naranja
        "enw" or "eht" or "ebo" or "edi"   => "#cc88ff", // huevos — lila
        "bct" or "pgt" or "pdr"
            or "pfk" or "pin" or "pex"     => "#999999", // recursos — gris
        "msz" or "tna" or "sgt"
            or "seg" or "smg"              => "#4499ff", // sistema — azul
        _                                  => "#ffffff",
    };
}
