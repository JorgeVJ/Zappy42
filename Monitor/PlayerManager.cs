using Godot;
using System.Collections.Generic;

public partial class PlayerManager : Node
{
	private Dictionary<int, Player> players = new();

	private Node3D playerContainer;

	public override void _Ready()
	{
		playerContainer = GetNodeOrNull<Node3D>("Players");
		if (playerContainer == null)
		{
			playerContainer = new Node3D();
			playerContainer.Name = "Players";
			AddChild(playerContainer);
		}
	}

	public Player GetOrCreate(int id, Vector3 pos, string teamName)
	{
		if (players.TryGetValue(id, out var existing))
			return existing;

		var p = Player.Create(pos);
		p.Init(id, teamName);

		playerContainer.AddChild(p);
		players[id] = p;

		return p;
	}

	public bool TryGet(int id, out Player player)
		=> players.TryGetValue(id, out player);

	public void Remove(int id)
	{
		if (!players.TryGetValue(id, out var p))
			return;

		p.QueueFree();
		players.Remove(id);
	}
}
