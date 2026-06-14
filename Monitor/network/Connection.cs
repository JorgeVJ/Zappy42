using Godot;

// Hub central: cablea los componentes (transporte, dispatcher, managers, UI) y
// mantiene el estado compartido entre los handlers del protocolo (repartidos en
// Connection.Players.cs / Connection.Eggs.cs / Connection.System.cs).
//
// El transporte (socket real o MockServer) vive en ServerTransport, que emite
// LineReceived(line) por cada línea completa del protocolo; este hub la pasa al
// MessageDispatcher, que enruta por comando a los métodos handler de las clases
// parciales. La selección por click vive en SelectionController.
public partial class Connection : Node
{
    private PlayerManager playerManager;
    private EggManager eggManager;
    private Terrain terrainManager;

    [Export]
    private InventoryPanel inventoryPanel;

    private MessageLogPanel _logPanel;
    private TeamProgressPanel _teamPanel;
    private SpeedControlPanel _speedPanel;
    private TimelineBar _timelineBar;

    private Camera camera;

    private ServerTransport _transport;
    private MessageDispatcher _dispatcher;
    private SelectionController _selectionController;

    // Pon a true para usar el servidor simulado sin intentar conexión real
    [Export] public bool UseMockServer = true;

    private CrowdSystem _crowd;

    public override void _Ready()
    {
        playerManager = GetNode<PlayerManager>("PlayerManager");
        terrainManager = GetParent().GetNode<Terrain>("Terrain");
        camera = GetParent().GetNode<Camera>("Camera");
        eggManager = GetNode<EggManager>("EggManager");

        _selectionController = new SelectionController(terrainManager, inventoryPanel);
        camera.OnLeftClick += _selectionController.HandleLeftClick;

        var followBehavior = new CameraFollowBehavior();
        camera.AddChild(followBehavior);

        // Posicionamiento dinámico (steering) de los jugadores dentro de su tile.
        _crowd = new CrowdSystem();
        AddChild(_crowd);
        _crowd.Setup(playerManager, terrainManager);

        _logPanel = new MessageLogPanel();
        AddChild(_logPanel);

        _teamPanel = new TeamProgressPanel();
        AddChild(_teamPanel);
        _teamPanel.PlayerSelected += id =>
        {
            if (playerManager.TryGet(id, out var p))
            {
                _selectionController.ShowInventory(p);
                followBehavior.StartFollowing(p);
            }
        };

        _speedPanel = GetNode<SpeedControlPanel>("SpeedControlPanel");
        _speedPanel.SpeedChanged += OnSpeedChanged;

        _timelineBar = GetNode<TimelineBar>("TimelineBar");

        // Transporte: decide internamente mock vs. socket real (ParseConnectionArgs)
        // y nos avisa con UseMockServer cuál fue el resultado (p. ej. para el log inicial).
        _transport = new ServerTransport();
        _transport.UseMockServer = UseMockServer;
        AddChild(_transport);
        UseMockServer = _transport.UseMockServer;

        _transport.LineReceived += OnLineReceived;
        _transport.Disconnected += OnTransportDisconnected;

        _dispatcher = new MessageDispatcher();
        RegisterSystemHandlers(_dispatcher);
        RegisterPlayerHandlers(_dispatcher);
        RegisterEggHandlers(_dispatcher);

        _timeline = new TimelineController(this, _dispatcher);
        _timelineBar.Setup(_timeline);
    }

    // Línea completa del protocolo (real o mock): loguear y entregar a la
    // barra de tiempo, que decide si se despacha en vivo o solo se acumula.
    private void OnLineReceived(string line)
    {
        var parts = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        _logPanel.Log(parts[0], line);
        _timeline.OnLineReceived(line);
    }

    private void OnTransportDisconnected(string reason)
    {
        _logPanel?.Log("NET", $"Servidor desconectado: {reason}");
    }

    public void SendMessage(string msg)
    {
        // Durante el replay instantáneo de la barra de tiempo no se reenvían
        // comandos al servidor (mct, sgt, GRAPHIC...): solo se reproduce el
        // log ya recibido.
        if (ReplayInstant)
            return;

        _transport.SendMessage(msg);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.F2)
                _logPanel.Toggle();
            else if (key.Keycode == Key.F3)
                _teamPanel.Toggle();
        }
    }

    // --- Helpers compartidos para los handlers del protocolo (network/Connection*.cs) ---

    // Comprueba que el mensaje tenga al menos `minLength` tokens (incluyendo
    // parts[0], el comando). Si no, loguea un warning con el comando y la
    // longitud recibida y devuelve false: el handler debe hacer `return` sin
    // procesar el mensaje. Evita IndexOutOfRangeException con mensajes cortos
    // o malformados.
    private static bool RequireLength(string[] parts, int minLength, string command)
    {
        if (parts.Length >= minLength)
            return true;

        Log.Warn($"[{command}] Mensaje malformado: se esperaban al menos {minLength} campos, llegaron {parts.Length}.");
        return false;
    }

    // int.TryParse con log de error si falla. Usar en lugar de int.Parse directo
    // sobre campos del mensaje: un valor no numérico (mensaje corrupto/malformado)
    // se descarta sin lanzar FormatException.
    private static bool TryParseField(string text, string command, string fieldName, out int value)
    {
        if (int.TryParse(text, out value))
            return true;

        Log.Error($"[{command}] Campo '{fieldName}' inválido: \"{text}\" no es un entero.");
        return false;
    }
}
