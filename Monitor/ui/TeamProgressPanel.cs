using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TeamProgressPanel : CollapsiblePanel
{
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

    public override void _Ready()
    {
        Vector2 vp = GetViewportRect().Size;

        Setup("Equipos",
            new Rect2(vp.X - 330, 10, 320, 420),
            minimizedIcon: "teams");

        ScrollContainer scroll = new ScrollContainer();
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

    public void RegisterTeam(string teamName)
    {
        if (_teams.ContainsKey(teamName)) return;
        _teams[teamName] = new List<PlayerEntry>();
        _dirty = true;
    }

    /// <summary>
    /// Registra un jugador en su equipo. Si ya existía una entrada para el
    /// mismo id (p. ej. tras una reconexión) se reemplaza en vez de duplicarse.
    /// </summary>
    public void AddPlayer(int id, string team, int level)
    {
        if (!_teams.ContainsKey(team))
            _teams[team] = new List<PlayerEntry>();

        _playerTeam[id] = team;
        List<PlayerEntry> list = _teams[team];
        list.RemoveAll(p => p.Id == id);
        list.Add(new PlayerEntry { Id = id, Level = level, LastAction = "" });
        _dirty = true;
    }

    public void SetLevel(int id, int level)
    {
        if (!_playerTeam.TryGetValue(id, out string team)) return;
        List<PlayerEntry> list = _teams[team];
        int idx = list.FindIndex(p => p.Id == id);
        if (idx < 0) return;
        PlayerEntry entry = list[idx];
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
        List<PlayerEntry> list = _teams[team];
        int idx = list.FindIndex(p => p.Id == id);
        if (idx < 0) return;
        PlayerEntry entry = list[idx];
        entry.LastAction = action;
        list[idx] = entry;
        _dirty = true;
    }

    public void ShowWinner(string teamName)
    {
        HideWinner();

        CanvasLayer canvas = new CanvasLayer();
        canvas.Layer = 10;
        GetTree().CurrentScene.AddChild(canvas);

        ColorRect bg = new ColorRect();
        bg.Color = new Color(0f, 0f, 0f, 0.72f);
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(bg);

        Label lbl = new Label();
        lbl.Text                = $"🏆  {teamName}  GANA";
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.VerticalAlignment   = VerticalAlignment.Center;
        lbl.AddThemeFontSizeOverride("font_size", 48);
        lbl.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(lbl);

        _winnerCanvas = canvas;
    }

    /// <summary>Quita el overlay de "equipo ganador" (si está visible).</summary>
    public void HideWinner()
    {
        if (_winnerCanvas != null)
        {
            _winnerCanvas.QueueFree();
            _winnerCanvas = null;
        }
    }

    /// <summary>
    /// Vuelve el panel a su estado inicial (sin equipos/jugadores), usado por
    /// TimelineController al resetear el mundo para reproducir el log desde 0.
    /// </summary>
    public void Reset()
    {
        _teams.Clear();
        _playerTeam.Clear();
        HideWinner();
        _dirty = true;
    }

    private void Rebuild()
    {
        foreach (Node child in _teamsContainer.GetChildren())
            child.QueueFree();

        string leadingTeam = FindLeadingTeam();

        foreach (KeyValuePair<string, List<PlayerEntry>> entry in _teams)
        {
            string teamName = entry.Key;
            List<PlayerEntry> players = entry.Value;
            bool isLeading = teamName == leadingTeam && players.Count > 0;

            BuildTeamHeader(teamName, players, isLeading);
            foreach (PlayerEntry p in players.OrderByDescending(p => p.Level))
                BuildPlayerRow(p);

            _teamsContainer.AddChild(new HSeparator());
        }
    }

    /// <summary>Equipo líder = mayor nivel máximo entre sus jugadores.</summary>
    private string FindLeadingTeam()
    {
        return _teams
            .Where(kv => kv.Value.Count > 0)
            .OrderByDescending(kv => kv.Value.Max(p => p.Level))
            .Select(kv => kv.Key)
            .FirstOrDefault();
    }

    private void BuildTeamHeader(string teamName, List<PlayerEntry> players, bool isLeading)
    {
        int maxLevel = players.Count > 0 ? players.Max(p => p.Level) : 0;

        RichTextLabel teamHeader = new RichTextLabel();
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
    }

    /// <summary>Fila de un jugador: botón de selección "#1  Nv.4" + última acción.</summary>
    private void BuildPlayerRow(PlayerEntry p)
    {
        HBoxContainer row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        _teamsContainer.AddChild(row);

        Button btn = new Button();
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

        row.AddChild(new VSeparator());

        Label actionLbl = new Label();
        actionLbl.Text                = p.LastAction ?? "";
        actionLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        actionLbl.ClipText            = true;
        actionLbl.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        row.AddChild(actionLbl);
    }

    /// <summary>Color según nivel: naranja (7+), amarillo (5+), verde (3+), gris (resto).</summary>
    private static Color GetLevelColor(int level) =>
        level >= 7 ? new Color(1f,    0.53f, 0f)
      : level >= 5 ? new Color(1f,    0.93f, 0f)
      : level >= 3 ? new Color(0.27f, 1f,   0.53f)
      :              new Color(0.67f, 0.67f, 0.67f);
}
