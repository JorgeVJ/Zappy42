using Godot;
using System.Collections.Generic;

/// <summary>
/// Estilo unificado para los botones de chrome/control del monitor: icono SVG
/// centrado, tamaño fijo y fondo redondeado translúcido que se ilumina al hover.
/// </summary>
/// <remarks>
/// Helper estático y genérico (mismo patrón que el resto de helpers en código):
/// se aplica desde los controladores en _Ready(), sin tocar uids en los .tscn.
/// Los iconos viven en res://ui/icons/&lt;name&gt;.svg (blancos, 24x24).
/// </remarks>
public static class IconButton
{
    public const float Size = 36f;

    private const int CornerRadius  = 6;
    private const int ContentMargin = 7;

    private static readonly Color BgNormal   = new Color(0.10f, 0.10f, 0.12f, 0.55f);
    private static readonly Color BgHover    = new Color(0.22f, 0.22f, 0.26f, 0.80f);
    private static readonly Color BgPressed  = new Color(0.06f, 0.06f, 0.08f, 0.90f);
    private static readonly Color BgDisabled = new Color(0.10f, 0.10f, 0.12f, 0.30f);

    private static readonly Color IconNormal   = Colors.White;
    private static readonly Color IconDisabled = new Color(1f, 1f, 1f, 0.35f);

    /// <summary>
    /// Caché por nombre para no recargar la textura en cada frame (ej. el swap
    /// play/pause de TimelineBar._Process).
    /// </summary>
    private static readonly Dictionary<string, Texture2D> _icons = new();

    public static Texture2D Load(string name)
    {
        if (!_icons.TryGetValue(name, out Texture2D tex))
        {
            tex = GD.Load<Texture2D>($"res://ui/icons/{name}.svg");
            _icons[name] = tex;
        }
        return tex;
    }

    /// <summary>
    /// Tamaño + estilo, sin icono. Para botones cuyo icono se asigna aparte o
    /// cambia en runtime (Play/Pause, Mute).
    /// </summary>
    public static void Style(Button btn)
    {
        btn.Text              = "";
        btn.CustomMinimumSize = new Vector2(Size, Size);
        btn.ExpandIcon        = true;
        btn.IconAlignment     = HorizontalAlignment.Center;
        btn.Flat              = false;
        btn.FocusMode         = Control.FocusModeEnum.None;

        btn.AddThemeStyleboxOverride("normal",   MakeBox(BgNormal));
        btn.AddThemeStyleboxOverride("hover",    MakeBox(BgHover));
        btn.AddThemeStyleboxOverride("pressed",  MakeBox(BgPressed));
        btn.AddThemeStyleboxOverride("disabled", MakeBox(BgDisabled));

        btn.AddThemeColorOverride("icon_normal_color",   IconNormal);
        btn.AddThemeColorOverride("icon_hover_color",    IconNormal);
        btn.AddThemeColorOverride("icon_pressed_color",  IconNormal);
        btn.AddThemeColorOverride("icon_disabled_color", IconDisabled);
    }

    /// <summary>Estilo + icono fijo (Cerrar, Limpiar, Live).</summary>
    public static void Apply(Button btn, string iconName, string tooltip = null)
    {
        Style(btn);
        btn.Icon = Load(iconName);
        if (tooltip != null)
            btn.TooltipText = tooltip;
    }

    private static StyleBoxFlat MakeBox(Color bg)
    {
        StyleBoxFlat box = new StyleBoxFlat { BgColor = bg };
        box.SetCornerRadiusAll(CornerRadius);
        box.SetContentMarginAll(ContentMargin);
        return box;
    }
}
