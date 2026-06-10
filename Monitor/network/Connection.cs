using Godot;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using zappy;

public partial class Connection : Node
{
	private TcpClient _client;
	private NetworkStream _stream;
	private byte[] _buffer = new byte[4096];

	// Destino de conexión real; sobrescrito por los flags -h/-p (ver ParseConnectionArgs).
	private string _host = "127.0.0.1";
	private int _port = 12345;

	private PlayerManager playerManager;
	private EggManager eggManager;
	private Terrain terrainManager;

	private List<string> teams = new List<string>();

	// Jugadores incantando por tile (pic), para terminar su animación al recibir pie.
	private readonly Dictionary<(int, int), List<int>> _incantations = new();

	[Export]
	private InventoryPanel inventoryPanel;

	private ISelectable selection;

	private MockServer _mockServer;
	private MessageLogPanel _logPanel;
	private TeamProgressPanel _teamPanel;
	private SpeedControlPanel _speedPanel;

	private Camera camera;

	// Pon a true para usar el servidor simulado sin intentar conexión real
	[Export] public bool UseMockServer = true;

	public override void _Ready()
	{
		playerManager = GetNode<PlayerManager>("PlayerManager");
		terrainManager = GetParent().GetNode<Terrain>("Terrain");
		camera = GetParent().GetNode<Camera>("Camera");
		camera.OnLeftClick += HandleLeftClick;
		var followBehavior = new CameraFollowBehavior();
		camera.AddChild(followBehavior);
		eggManager = GetNode<EggManager>("EggManager");

		_logPanel = new MessageLogPanel();
		AddChild(_logPanel);

		_teamPanel = new TeamProgressPanel();
		AddChild(_teamPanel);
		_teamPanel.PlayerSelected += id =>
		{
			if (playerManager.TryGet(id, out var p))
			{
				ShowInventory(p);
				followBehavior.StartFollowing(p);
			}
		};

		_speedPanel = GetNode<SpeedControlPanel>("SpeedControlPanel");
		_speedPanel.SpeedChanged += OnSpeedChanged;

		ParseConnectionArgs();

		if (UseMockServer)
		{
			_mockServer = new MockServer();
			GD.Print("[Connection] Modo mock activo — sin conexión TCP.");
			return;
		}

		try
		{
			_client = new TcpClient();
			_client.Connect(_host, _port);
			_stream = _client.GetStream();

			GD.Print($"[Connection] Conectado a {_host}:{_port}. Esperando WELCOME...");
			// El GRAPHIC se envía al recibir WELCOME (handshake Zappy), no al conectar.
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[Connection] Error al conectar a {_host}:{_port}: {ex.Message}");
			GD.PrintErr("[Connection] Uso: zappy_gui -p <puerto> -h <host> [--mock]");
		}
	}

	// Lee los flags de línea de comandos (-p puerto, -h host, --mock) para soportar
	// el arranque exigido por el subject: zappy_gui -p <port> -h <host>.
	// Si se pasan -p/-h válidos se fuerza la conexión real; --mock siempre gana.
	private void ParseConnectionArgs()
	{
		var args = new List<string>();
		args.AddRange(OS.GetCmdlineUserArgs());
		args.AddRange(OS.GetCmdlineArgs());

		bool hasConnArgs = false;
		bool forceMock = false;

		for (int i = 0; i < args.Count; i++)
		{
			switch (args[i])
			{
				case "-p" when i + 1 < args.Count:
					if (int.TryParse(args[i + 1], out int p))
					{
						_port = p;
						hasConnArgs = true;
					}
					else
					{
						GD.PrintErr($"[Connection] Puerto inválido: '{args[i + 1]}'");
					}
					break;
				case "-h" when i + 1 < args.Count:
					_host = args[i + 1];
					hasConnArgs = true;
					break;
				case "--mock":
					forceMock = true;
					break;
			}
		}

		if (forceMock)
			UseMockServer = true;
		else if (hasConnArgs)
			UseMockServer = false;

		GD.Print($"[Connection] Args de conexión: host={_host}, port={_port}, mock={UseMockServer}");
	}

	private void HandleLeftClick(GodotObject collider, Vector3 position)
	{
		GD.Print($"Entra en HandleLeftClick: {position}");

		Node node = collider as Node;

		while (node != null)
		{
			if (node is Player player)
			{
				GD.Print("Colisiona con Player");
				terrainManager.DeselectTile();
				PlayerClicked(player);
				return;
			}

			if (node is Resource)
			{
				GD.Print("Colisiona con Recurso");

				// Clicar un recurso muestra el inventario de la casilla que lo contiene.
				Tile resourceTile = terrainManager.GetTileFromPosition(position);
				if (resourceTile != null)
				{
					terrainManager.SelectTile(resourceTile.Coord.X, resourceTile.Coord.Y);
					ShowInventory(resourceTile);
				}
				else
				{
					terrainManager.DeselectTile();
				}
				return;
			}

			node = node.GetParent();
		}

		// Si no es entidad → terreno
		GD.Print("Colisiona con Terreno");

		Tile tile = terrainManager.GetTileFromPosition(position);

		if (tile != null)
		{
			terrainManager.SelectTile(tile.Coord.X, tile.Coord.Y);
			ShowInventory(tile);
		}
		else
		{
			terrainManager.DeselectTile();
		}
	}

	private void ShowInventory(object owner)
	{
		selection?.UnHightlight();

		if (owner is ISelectable selectable)
		{
			selection = selectable;
			selectable.Highlight();
		}
		else
		{
			selection = null;
		}

		if (owner is IInventory inventoryOwner)
		{
			inventoryPanel.ShowForTile(inventoryOwner);
		}
	}

	private void PlayerClicked(Player player)
	{
		ShowInventory(player);
	}

	public void SendMessage(string msg)
	{
		if (_stream == null)
		{
			return;
		}

		GD.Print($"Sending: {msg}");
		byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
		_stream.Write(data, 0, data.Length);
	}

	public override void _Process(double delta)
	{
		// Mockeo de mensajes para pruebas
		if (_mockServer != null)
		{
			string mockMsg = _mockServer.GetNextCommand(delta);
			if (!string.IsNullOrEmpty(mockMsg))
			{
				GD.Print("[MOCK] " + mockMsg);
				HandleServerMessage(mockMsg);
			}
			return;
		}

		if (_stream == null || !_stream.DataAvailable)
		{
			return;
		}

		byte[] buffer = new byte[_client.Available];
		int bytesRead = _stream.Read(buffer, 0, buffer.Length);

		string msg;
		try
		{
			// Forzar excepción si hay bytes inválidos para UTF-8, así los detectamos y los registramos.
			msg = new System.Text.UTF8Encoding(false, true).GetString(buffer, 0, bytesRead);
		}
		catch (System.Text.DecoderFallbackException)
		{
			// Loguear los bytes crudos en hex para depuración
			GD.PrintErr($"Unicode parsing error: invalid UTF-8 bytes recibidos. Raw: {BitConverter.ToString(buffer, 0, bytesRead)}");

			// Intentar decodificar con el fallback permissivo para seguir procesando (reemplaza por �)
			msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
		}

		string[] lines = msg.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		foreach (string line in lines)
		{
			GD.Print("Processing line: " + line);
			HandleServerMessage(line);
		}
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

	private void HandleServerMessage(string line)
	{
		var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return;
		}

		_logPanel.Log(parts[0], line);

		switch (parts[0])
		{
			case "WELCOME": SendMessage("GRAPHIC"); break; // handshake: el servidor saluda; respondemos GRAPHIC
			case "msz": msz(parts); break; // msz X Y\n msz\n Map size
			case "bct": bct(parts); break; // bct X Y q q q q q q q\n bct X Y\n Contents of a map tile
			case "tna": tna(parts); break; // tna N\n(× nbr teams) tna\n Team names
			case "pnw": pnw(parts); break; // pnw #n X Y O L N\n - New player connection
			case "ppo": ppo(parts); break; // ppo #n X Y O\n ppo #n\n Player position
			case "plv": plv(parts); break; // plv #n L\n plv #n\n Player level
			case "pin": pin(parts); break; // pin #n X Y q q q q q q q\n pin #n\n Player inventory
			case "pex": pex(parts); break; // pex #n\n - A player is expelled
			case "pbc": pbc(parts); break; // pbc #n M\n - A player broadcasts
			case "pic": pic(parts); break; // pic X Y L #n #n ...\n - Incantation started on the tile by a player
			case "pie": pie(parts); break; // pie X Y R\n - End of incantation with result R(0 or 1)
			case "pfk": pfk(parts); break; // pfk #n\n - The player lays an egg
			case "pdr": pdr(parts); break; // pdr #n i\n - The player drops a resource
			case "pgt": pgt(parts); break; // pgt #n i\n - The player takes a resource
			case "pdi": pdi(parts); break; // pdi #n\n - The player dies of hunger
			case "enw": enw(parts); break; // enw #e #n X Y\n - The egg is laid on the tile by a player
			case "eht": eht(parts); break; // eht #e\n - The egg hatches
			case "ebo": ebo(parts); break; // ebo #e\n - A player connects for an egg
			case "edi": edi(parts); break; // edi #e\n - The hatched egg dies of hunger
			case "sgt": sgt(parts); break; // sgt T\n sgt\n Request for current time unit
			case "seg": seg(parts); break; // seg N\n - End of game, team N wins
			case "smg": smg(parts); break; // smg M\n - Server message
			case "suc": suc(parts); break; // suc\n - Unknown command
			case "sbp": sbp(parts); break; // sbp\n - Bad parameters for the command

			default:
				GD.Print("Mensaje desconocido: " + line);
				break;
		}
	}

	private void sbp(string[] parts)
	{
		GD.Print("[sbp] Parámetros inválidos en comando enviado al servidor");
	}

	private void suc(string[] parts)
	{
		GD.Print("[suc] Comando desconocido recibido del servidor");
	}

	private void smg(string[] parts)
	{
		// smg M — mensaje de texto informativo del servidor (puede contener espacios).
		// NO implica fin de partida ni debe pausar la escena (antes era copia de seg).
		if (parts.Length < 2)
			return;

		string message = string.Join(" ", parts, 1, parts.Length - 1);
		GD.Print($"[smg] {message}");
		// El mensaje ya queda visible en MessageLogPanel vía HandleServerMessage.
	}

	private void seg(string[] parts)
	{
		string winner = parts[1];
		GD.Print($"[seg] ¡Juego terminado! Equipo ganador: {winner}");
		_teamPanel?.ShowWinner(winner);
		GetTree().Paused = true;
	}

	private void OnSpeedChanged(int t)
	{
		if (_mockServer != null)
			_mockServer.SetSpeed(t);
		else
			SendMessage($"sst {t}");
	}

	private void sgt(string[] parts)
	{
		int tick = int.Parse(parts[1]);
		GD.Print($"[sgt] Tiempo actual del servidor: {tick}");
		_speedPanel?.SetDisplayValue(tick);
	}

	private void edi(string[] parts)
	{
		// edi #e
		int eggId = int.Parse(parts[1].TrimStart('#'));

		if (!eggManager.TryGet(eggId, out var egg))
		{
			GD.PrintErr($"[edi] Egg #{eggId} no existe.");
			return;
		}

		eggManager.Remove(eggId);

		GD.Print($"[edi] Egg #{eggId} murio de hambre.");
	}

	private void ebo(string[] parts)
	{
		// ebo #e
		int eggId = int.Parse(parts[1].TrimStart('#'));

		if (!eggManager.TryGet(eggId, out var egg))
		{
			GD.PrintErr($"[ebo] Egg #{eggId} no existe.");
			return;
		}

		// eliminamos el huevo porque el jugador se conecta
		eggManager.Remove(eggId);

		GD.Print($"[ebo] Egg #{eggId} ahora es controlado por un jugador (se conecto).");
	}

	private void eht(string[] parts)
	{
		// eht #e
		int eggId = int.Parse(parts[1].TrimStart('#'));

		if (!eggManager.TryGet(eggId, out var egg))
		{
			GD.PrintErr($"[eht] Egg #{eggId} no existe.");
			return;
		}

		eggManager.Remove(eggId);

		GD.Print($"[eht] Egg #{eggId} ha eclosionado.");
	}

	private void enw(string[] parts)
	{
		// enw #e #n X Y
		int eggId = int.Parse(parts[1].TrimStart('#'));
		int playerId = int.Parse(parts[2].TrimStart('#'));
		int x = int.Parse(parts[3]);
		int y = int.Parse(parts[4]);

		Vector3 worldPos = TerrainSnap.TileCenter(terrainManager, x, y, 0.15f);

		var egg = eggManager.CreateEgg(eggId, worldPos);

		GD.Print($"[enw] Egg #{eggId} puesto por Player #{playerId} en ({x},{y})");
	}

	private void pdi(string[] parts)
	{
		// pdi #n
		int id = int.Parse(parts[1].TrimStart('#'));

		if (!playerManager.TryGet(id, out var player))
		{
			GD.PrintErr($"[pdi] Player #{id} no existe.");
			return;
		}

		playerManager.Remove(id);
		_teamPanel?.RemovePlayer(id);

		GD.Print($"[pdi] Player #{id} murio (eliminado).");
	}

	private void pgt(string[] parts)
	{
		// pgt #n i
		int id = int.Parse(parts[1].TrimStart('#'));
		int itemId = int.Parse(parts[2]);

		var type = (Resource.ResourceType)itemId;

		if (!playerManager.TryGet(id, out var player))
		{
			GD.PrintErr($"[pgt] Player #{id} no existe todavia.");
			return;
		}

		// +1 al inventario local (si lo estás usando)
		player.Inventory.Add(type, 1);
		var tilePos = player.TilePos;
		terrainManager[tilePos.X, tilePos.Y]?.Inventory.Remove(type, 1);
		_teamPanel?.SetLastAction(id, $"+ {type}");
		GD.Print($"[pgt] Player #{id} tomo {type}");
	}

	private void pdr(string[] parts)
	{
		// pdr #n i
		int id = int.Parse(parts[1].TrimStart('#'));
		int itemId = int.Parse(parts[2]);

		var type = (Resource.ResourceType)itemId;

		if (!playerManager.TryGet(id, out var player))
		{
			GD.PrintErr($"[pdr] Player #{id} no existe todavia.");
			return;
		}

		player.Inventory.Remove(type, 1);

		_teamPanel?.SetLastAction(id, $"- {type}");
		GD.Print($"[pdr] Player #{id} dejo {type}");
	}

	private void pfk(string[] parts)
	{
		// pfk #n
		int playerId = int.Parse(parts[1].TrimStart('#'));

		if (!playerManager.TryGet(playerId, out var player))
		{
			GD.PrintErr($"[pfk] Player #{playerId} no encontrado");
			return;
		}

		GD.Print($"[pfk] Player #{playerId} esta poniendo un huevo");
		_teamPanel?.SetLastAction(playerId, "🥚 pone huevo");

		// Highlight del tile donde está el jugador
		Vector2I tilePos = player.TilePos;

		//if (tilePos.X < 0 || tilePos.X >= mapW || tilePos.Y < 0 || tilePos.Y >= mapH)
		//{
		//	return;
		//}

		var tile = terrainManager[tilePos.X, tilePos.Y];

		// Color "pre-huevo" (verde amarillento)
		//tile.Highlight();

		// Opcional: feedback visual en el jugador
		// player.Flash(new Color(1f, 1f, 0.4f));
	}

	private async System.Threading.Tasks.Task FadeOutTile(Node3D mesh, float seconds)
	{
		await System.Threading.Tasks.Task.Delay((int)(seconds * 1000));
		if (mesh != null && mesh.IsInsideTree())
		{
			mesh.QueueFree();
		}
	}

	private void pie(string[] parts)
	{
		// pie X Y R
		if (parts.Length < 4)
			return;

		int x = int.Parse(parts[1]);
		int y = int.Parse(parts[2]);
		int result = int.Parse(parts[3]);
		bool success = result == 1;

		GD.Print($"[pie] Incantacion en tile ({x},{y}) {(success ? "EXITOSA" : "FALLIDA")}");

		// Terminar la animación de hechizo de los jugadores que estaban incantando aquí.
		if (_incantations.TryGetValue((x, y), out var playerIds))
		{
			foreach (int pid in playerIds)
			{
				if (playerManager.TryGet(pid, out var player))
					player.StopSpell();
			}
			_incantations.Remove((x, y));
		}

		// Quitar el resaltado del tile y mostrar un pulso de color según el resultado
		// (verde = éxito, rojo = fallo) reutilizando el efecto SoundWave.
		terrainManager?.DeselectTile();
		ShowIncantationResult(x, y, success);
	}

	private void pic(string[] parts)
	{
		// pic X Y L #n #n ...
		if (parts.Length < 4)
			return;

		int x = int.Parse(parts[1]);
		int y = int.Parse(parts[2]);
		int level = int.Parse(parts[3]);

		// jugadores involucrados
		List<int> playerIds = new List<int>();
		for (int i = 4; i < parts.Length; i++)
		{
			playerIds.Add(int.Parse(parts[i].TrimStart('#')));
		}

		GD.Print($"[pic] Incantacion en tile ({x},{y}) nivel {level} con jugadores: {string.Join(",", playerIds)}");

		_incantations[(x, y)] = playerIds;

		foreach (int pid in playerIds)
		{
			_teamPanel?.SetLastAction(pid, $"✨ incant. Nv.{level}");
			if (playerManager.TryGet(pid, out var player))
				player.PlaySpell();
		}

		// Resaltar el tile de la incantación (reutiliza el shader de selección del terreno).
		if (terrainManager != null && terrainManager[x, y] != null)
			terrainManager.SelectTile(x, y);
	}

	// Pulso de color sobre el tile al terminar una incantación (verde éxito / rojo fallo).
	private void ShowIncantationResult(int x, int y, bool success)
	{
		if (terrainManager == null || terrainManager[x, y] == null)
			return;

		Color color = success ? new Color(0.3f, 1f, 0.4f, 0.85f) : new Color(1f, 0.3f, 0.3f, 0.85f);
		Vector3 center = TerrainSnap.TileCenter(terrainManager, x, y, 0.15f);
		var wave = SoundWave.Create(center, color);
		terrainManager.AddChild(wave);
	}

	private async void ShowPlayerMessage(Player player, string msg)
	{
		var label = new Label3D();
		label.Text = msg;
		label.Position = new Vector3(0, 1.5f, 0); // encima del jugador
		player.AddChild(label);

		// desaparecer después de 2 segundos
		await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
		label.QueueFree();
	}

	private void pbc(string[] parts)
	{
		// pbc #n M
		int id = int.Parse(parts[1].TrimStart('#'));

		if (!playerManager.TryGet(id, out var player))
		{
			GD.PrintErr($"[pbc] Player #{id} no existe.");
			return;
		}

		// reconstruimos el mensaje (puede contener espacios)
		string message = string.Join(" ", parts, 2, parts.Length - 2);

		GD.Print($"[pbc] Player #{id} dice: {message}");
		_teamPanel?.SetLastAction(id, $"📢 {message}");
		ShowPlayerMessage(player, message);
		ShowSoundWave(player);
	}

	// Expanding ground ring centered on the emitter's tile, visualizing the
	// broadcast as sound propagating outward. Complements the floating text.
	private void ShowSoundWave(Player player)
	{
		if (terrainManager == null)
			return;

		Vector3 center = TerrainSnap.TileCenter(terrainManager, player.TilePos.X, player.TilePos.Y, 0.15f);
		var wave = SoundWave.Create(center);
		terrainManager.AddChild(wave);
	}

	private void pex(string[] parts)
	{
		// pex #n
		int id = int.Parse(parts[1].TrimStart('#'));

		if (!playerManager.TryGet(id, out var player))
		{
			GD.PrintErr($"[pex] Player #{id} no existe.");
			return;
		}

		GD.Print($"[pex] Player #{id} expulso a otros jugadores.");
		_teamPanel?.SetLastAction(id, "💨 expulsó");
	}

	private void pin(string[] parts)
	{
		// pin #n X Y q0 q1 q2 q3 q4 q5 q6
		int id = int.Parse(parts[1].TrimStart('#'));
		int x = int.Parse(parts[2]);
		int y = int.Parse(parts[3]);

		if (!playerManager.TryGet(id, out var player))
		{
			GD.PrintErr($"[pin] Player #{id} no existe todavía.");
			return;
		}

		// Posición (pin SIEMPRE incluye posición)
		player.SetTilePos(x, y);

		// Inventario
		var inv = player.Inventory;

		inv.Set(Resource.ResourceType.Nourriture, int.Parse(parts[4]));
		inv.Set(Resource.ResourceType.Linemate, int.Parse(parts[5]));
		inv.Set(Resource.ResourceType.Deraumere, int.Parse(parts[6]));
		inv.Set(Resource.ResourceType.Sibur, int.Parse(parts[7]));
		inv.Set(Resource.ResourceType.Mendiane, int.Parse(parts[8]));
		inv.Set(Resource.ResourceType.Phiras, int.Parse(parts[9]));
		inv.Set(Resource.ResourceType.Thystame, int.Parse(parts[10]));

		GD.Print($"[pin] Player #{id} inventario actualizado");
	}

	private void plv(string[] parts)
	{
		// plv #n L
		int id = int.Parse(parts[1].TrimStart('#'));
		int level = int.Parse(parts[2]);

		if (!playerManager.TryGet(id, out var player))
		{
			GD.PrintErr($"[plv] Player #{id} no existe todavia.");
			return;
		}

		player.SetLevel(level);
		_teamPanel?.SetLevel(id, level);

		GD.Print($"[plv] Player #{id} -> level {level}");
	}

	private void ppo(string[] parts)
	{
		// ppo #n X Y O
		int id = int.Parse(parts[1].TrimStart('#'));
		int x = int.Parse(parts[2]);
		int y = int.Parse(parts[3]);
		int o = int.Parse(parts[4]);

		if (!playerManager.TryGet(id, out var player))
		{
			GD.PrintErr($"[ppo] Player #{id} no existe todavia.");
			return;
		}

		player.SetTilePos(x, y);
		player.SetOrientation(o);
		_teamPanel?.SetLastAction(id, $"→ ({x},{y})");

		// GD.Print($"[ppo] Player #{id} -> ({x},{y}) o={o}");
	}

	private void tna(string[] parts)
	{
		if (parts.Length < 2)
		{
			GD.PrintErr("[tna] Formato incorrecto.");
			return;
		}

		string teamName = parts[1];
		if (!teams.Contains(teamName))
		{
			teams.Add(teamName);
		}

		_teamPanel?.RegisterTeam(teamName);
		GD.Print($"[tna] Equipo registrado: {teamName}");
	}

	private void pnw(string[] parts)
	{
		// pnw #n X Y O L N
		int id = int.Parse(parts[1].TrimStart('#'));
		int x = int.Parse(parts[2]);
		int y = int.Parse(parts[3]);
		int o = int.Parse(parts[4]);
		int level = int.Parse(parts[5]);
		string team = parts[6];

		// convertir coords tile -> mundo (centro del tile)
		Vector3 worldPos = TerrainSnap.TileCenter(terrainManager, x, y, 0f);

		var player = playerManager.GetOrCreate(id, worldPos, team);

		// si ya existía, lo actualizamos también
		player.Position = worldPos;
		player.SetTerrain(terrainManager);
		player.SetOrientation(o);
		player.SetLevel(level);

		_teamPanel?.AddPlayer(id, team, level);
		GD.Print($"[pnw] Player #{id} team={team} pos=({x},{y}) o={o} lvl={level}");
	}

	private void bct(string[] parts)
	{
		int x = int.Parse(parts[1]);
		int y = int.Parse(parts[2]);

		// Recursos son lo que viene a partir del índice 3
		for (int i = 3; i < parts.Length; i++)
		{
			terrainManager[x, y].Inventory.Set((Resource.ResourceType)(i - 3), int.Parse(parts[i]));
		}
	}

	private void msz(string[] parts)
	{
		var mapW = int.Parse(parts[1]);
		var mapH = int.Parse(parts[2]);
		GD.Print($"Mapa de tamaño: {mapW} x {mapH}");
		//CreateMap();
		terrainManager.InitializeMap(mapW, mapH);
		SendMessage("mct");
	}
}
