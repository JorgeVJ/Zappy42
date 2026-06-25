using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TeamProgressPanel : CollapsiblePanel
{
    // ── Modelo de datos ───────────────────────────────────────────────────

    private struct PlayerEntry
    {
        public int    Id;
        public int    Level;
        public string LastAction;
    }

    /// <summary>Se dispara al pulsar el botón de un jugador. Parámetro: ID del jugador.</summary>
    public event Action<int> PlayerSelected;

    private readonly Dictionary<string, List<PlayerEntry>> _teams      = new();
    private readonly Dictionary<int, string>               _playerTeam = new();

    private VBoxContainer _teamsContainer;
    private bool          _dirty = false;
    private CanvasLayer   _winnerCanvas;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public override void _Ready()
    {
        var vp = GetViewportRect().Size;

        // Panel esquina superior derecha: 320x420, margen 10px
        Setup("Equipos",
            new Rect2(vp.X - 330, 10, 320, 420),
            Control.LayoutPreset.TopLeft,
            minimizedIcon: "teams");

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        Content.AddChild(scroll);

        _teamsContainer = new VBoxContainer();
        _teamsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_teamsContainer);
    }

    public override void _Process(double delta)
    {
        if (_dirty && _teamsContainer != null)
        {
            _dirty = false;
            Rebuild();
        }
    }

    // ── Métodos públicos (llamados desde Connection) ───────────────────────

    public void RegisterTeam(string teamName)
    {
        if (_teams.ContainsKey(teamName)) return;
        _teams[teamName] = new List<PlayerEntry>();
        _dirty = true;
    }

    public void AddPlayer(int id, string team, int level)
    {
        if (!_teams.ContainsKey(team))
            _teams[team] = new List<PlayerEntry>();

        _playerTeam[id] = team;
        var list = _teams[team];
        list.RemoveAll(p => p.Id == id); // evitar duplicado en reconexión
        list.Add(new PlayerEntry { Id = id, Level = level, LastAction = "" });
        _dirty = true;
    }

    public void SetLevel(int id, int level)
    {
        if (!_playerTeam.TryGetValue(id, out string team)) return;
        var list = _teams[team];
        int idx = list.FindIndex(p => p.Id == id);
        if (idx < 0) return;
        var entry = list[idx];
        entry.Level = level;
        list[idx] = entry;
        _dirty = true;
    }

    public void RemovePlayer(int id)
    {
        if (!_playerTeam.TryGetValue(id, out string team)) return;
        _teams[team].RemoveAll(p => p.Id == id);
        _playerTeam.Remove(id);
        _dirty = true;
    }

    public void SetLastAction(int id, string action)
    {
        if (!_playerTeam.TryGetValue(id, out string team)) return;
        var list = _teams[team];
        int idx = list.FindIndex(p => p.Id == id);
        if (idx < 0) return;
        var entry = list[idx];
        entry.LastAction = action;
        list[idx] = entry;
        _dirty = true;
    }

    public void ShowWinner(string teamName)
    {
        HideWinner();

        var canvas = new CanvasLayer();
        canvas.Layer = 10;
        GetTree().CurrentScene.AddChild(canvas);

        var bg = new ColorRect();
        bg.Color = new Color(0f, 0f, 0f, 0.72f);
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(bg);

        var lbl = new Label();
        lbl.Text                = $"🏆  {teamName}  GANA";
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.VerticalAlignment   = VerticalAlignment.Center;
        lbl.AddThemeFontSizeOverride("font_size", 48);
        lbl.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(lbl);

        _winnerCanvas = canvas;
    }

    // Quita el overlay de "equipo ganador" (si está visible).
    public void HideWinner()
    {
        if (_winnerCanvas != null)
        {
            _winnerCanvas.QueueFree();
            _winnerCanvas = null;
        }
    }

    // Vuelve el panel a su estado inicial (sin equipos/jugadores), usado por
    // TimelineController al resetear el mundo para reproducir el log desde 0.
    public void Reset()
    {
        _teams.Clear();
        _playerTeam.Clear();
        HideWinner();
        _dirty = true;
    }

    // ── Reconstrucción de la UI ────────────────────────────────────────────

    private void Rebuild()
    {
        foreach (Node child in _teamsContainer.GetChildren())
            child.QueueFree();

        // Equipo líder = mayor nivel máximo entre sus jugadores
        string leadingTeam = _teams
            .Where(kv => kv.Value.Count > 0)
            .OrderByDescending(kv => kv.Value.Max(p => p.Level))
            .Select(kv => kv.Key)
            .FirstOrDefault();

        foreach (var (teamName, players) in _teams)
        {
            bool isLeading = teamName == leadingTeam && players.Count > 0;
            int  maxLevel  = players.Count > 0 ? players.Max(p => p.Level) : 0;

            // ── Cabecera del equipo ──────────────────────────────────────
            var teamHeader = new RichTextLabel();
            teamHeader.BbcodeEnabled = true;
            teamHeader.FitContent    = true;
            teamHeader.AutowrapMode  = TextServer.AutowrapMode.Off;

            string teamColor = isLeading ? "#44ff88" : "#dddddd";
            string crown     = isLeading ? "👑 " : "";
            string stats     = players.Count > 0
                ? $"  {players.Count} jug.  |  Nv. máx: {maxLevel}"
                : "  sin jugadores";

            teamHeader.AppendText(
                $"[b][color={teamColor}]{crown}{teamName}[/color][/b]" +
                $"[color=#777777]{stats}[/color]");
            _teamsContainer.AddChild(teamHeader);

            // ── Una fila por jugador (orden descendente de nivel) ────────
            foreach (var p in players.OrderByDescending(p => p.Level))
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 6);
                _teamsContainer.AddChild(row);

                // Botón de selección: "#1  Nv.4"
                var btn = new Button();
                btn.Text                = $"#{p.Id}  Nv.{p.Level}";
                btn.Flat                = true;
                btn.Alignment           = HorizontalAlignment.Left;
                btn.CustomMinimumSize   = new Vector2(110, 0);
                btn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
                btn.AddThemeColorOverride("font_color",       GetLevelColor(p.Level));
                btn.AddThemeColorOverride("font_hover_color", Colors.White);
                int capturedId = p.Id;
                btn.Pressed += () => PlayerSelected?.Invoke(capturedId);
                row.AddChild(btn);

                // Divisor visual
                row.AddChild(new VSeparator());

                // Última acción (ocupa el espacio restante)
                var actionLbl = new Label();
                actionLbl.Text                = p.LastAction ?? "";
                actionLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                actionLbl.ClipText            = true;
                actionLbl.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
                row.AddChild(actionLbl);
            }

            _teamsContainer.AddChild(new HSeparator());
        }
    }

    private static Color GetLevelColor(int level) =>
        level >= 7 ? new Color(1f,    0.53f, 0f)    // naranja
      : level >= 5 ? new Color(1f,    0.93f, 0f)    // amarillo
      : level >= 3 ? new Color(0.27f, 1f,   0.53f)  // verde
      :              new Color(0.67f, 0.67f, 0.67f); // gris
}
