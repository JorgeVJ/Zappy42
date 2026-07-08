using Godot;

/// <summary>
/// Paleta y estilos compartidos del look "soft-3D" (neumorfismo) del panel de
/// configuración: superficies oscuras mate con sombra proyectada y borde-highlight
/// superior que simulan relieve, sin coste de shader.
/// </summary>
/// <remarks>
/// Helper estático (mismo patrón que ui/IconButton.cs): se aplica desde código en
/// _Ready(), sin recursos .tres ni uids en los .tscn. Las constantes de color las
/// reutiliza ToggleSwitch para mantener coherencia visual.
/// </remarks>
public static class Neumorphism
{
    /// <summary>Fondo base del panel y de los controles hundidos.</summary>
    public static readonly Color Surface = new Color(0.13f, 0.14f, 0.17f, 0.97f);

    /// <summary>Relieve/luz superior de las piezas elevadas.</summary>
    public static readonly Color Highlight = new Color(1f, 1f, 1f, 0.10f);

    /// <summary>Sombra proyectada que da sensación de profundidad.</summary>
    public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.55f);

    /// <summary>Pista del interruptor apagado (recessed).</summary>
    public static readonly Color TrackOff = new Color(0.09f, 0.10f, 0.12f, 1f);

    /// <summary>Pista del interruptor encendido (acento).</summary>
    public static readonly Color TrackOn = new Color(0.24f, 0.58f, 0.52f, 1f);

    /// <summary>Perilla del interruptor.</summary>
    public static readonly Color Knob = new Color(0.85f, 0.87f, 0.90f, 1f);

    private const int PanelRadius = 14;
    private const int PanelShadowSize = 10;

    /// <summary>
    /// Aplica el fondo neumórfico al contenedor raíz de un panel: esquinas
    /// redondeadas, borde-highlight superior y sombra proyectada abajo-derecha.
    /// </summary>
    public static void StylePanel(PanelContainer panel)
    {
        StyleBoxFlat box = new StyleBoxFlat { BgColor = Surface };
        box.SetCornerRadiusAll(PanelRadius);
        box.SetBorderWidthAll(0);
        box.BorderWidthTop = 1;
        box.BorderColor = Highlight;
        box.ShadowColor = Shadow;
        box.ShadowSize = PanelShadowSize;
        box.ShadowOffset = new Vector2(0f, 4f);
        box.SetContentMarginAll(4);
        panel.AddThemeStyleboxOverride("panel", box);
    }
}
