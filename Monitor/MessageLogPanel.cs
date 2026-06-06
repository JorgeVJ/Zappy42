using Godot;

public partial class MessageLogPanel : Control
{
    private RichTextLabel _richText;
    private int _entryCount = 0;
    private const int MaxLines = 80;

    public override void _Ready()
    {
        // Panel 400x300 anclado a la esquina inferior izquierda con 10px de margen
        AnchorLeft = 0f;
        AnchorTop = 1f;
        AnchorRight = 0f;
        AnchorBottom = 1f;
        OffsetLeft = 10f;
        OffsetRight = 410f;
        OffsetTop = -310f;
        OffsetBottom = -10f;

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        margin.AddChild(vbox);

        var header = new HBoxContainer();
        vbox.AddChild(header);

        var titleLabel = new Label();
        titleLabel.Text = "Mensajes del servidor";
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(titleLabel);

        var clearBtn = new Button();
        clearBtn.Text = "Limpiar";
        clearBtn.Pressed += () => { _richText.Clear(); _entryCount = 0; };
        header.AddChild(clearBtn);

        vbox.AddChild(new HSeparator());

        _richText = new RichTextLabel();
        _richText.BbcodeEnabled = true;
        _richText.ScrollFollowing = true;
        _richText.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(_richText);
    }

    public void Log(string command, string rawLine)
    {
        if (_entryCount >= MaxLines)
        {
            _richText.Clear();
            _entryCount = 0;
        }

        string color = GetColor(command);
        string time = System.DateTime.Now.ToString("HH:mm:ss");
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
