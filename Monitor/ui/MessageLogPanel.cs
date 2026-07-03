using Godot;

public partial class MessageLogPanel : CollapsiblePanel
{
    private RichTextLabel _richText;
    private int _entryCount = 0;
    private const int MaxLines = 80;

    public override void _Ready()
    {
        Vector2 vp = GetViewportRect().Size;

        Setup("Mensajes del servidor",
            new Rect2(10, vp.Y - 350, 400, 300),
            minimizedIcon: "messages");

        _richText = new RichTextLabel();
        _richText.BbcodeEnabled    = true;
        _richText.ScrollFollowing  = true;
        _richText.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        Content.AddChild(_richText);

        HBoxContainer footer = new HBoxContainer();
        footer.Alignment = BoxContainer.AlignmentMode.End;
        Button clearBtn = new Button();
        IconButton.Apply(clearBtn, "clear", "Limpiar");
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

    /// <summary>
    /// Color BBCode por comando: cyan para spawn, rojo para muerte, blanco
    /// suave para movimiento, verde para nivel, amarillo para broadcast,
    /// naranja para incantación, lila para huevos, gris para recursos y azul
    /// para mensajes de sistema.
    /// </summary>
    private static string GetColor(string cmd) => cmd switch
    {
        "pnw"                              => "#00e5ff",
        "pdi"                              => "#ff4444",
        "ppo"                              => "#dddddd",
        "plv"                              => "#44ff88",
        "pbc"                              => "#ffee00",
        "pic" or "pie"                     => "#ff8800",
        "enw" or "eht" or "ebo" or "edi"   => "#cc88ff",
        "bct" or "pgt" or "pdr"
            or "pfk" or "pin" or "pex"     => "#999999",
        "msz" or "tna" or "sgt"
            or "seg" or "smg"              => "#4499ff",
        _                                  => "#ffffff",
    };
}
