using Godot;
using System;
using System.Collections.Generic;

public partial class Inventory : Node
{
	private Dictionary<Resource.ResourceType, int> data = new();

	public event Action Changed;

	public override void _Ready()
	{
		foreach (Resource.ResourceType t in Enum.GetValues(typeof(Resource.ResourceType)))
			data[t] = 0;
	}

	public void Set(Resource.ResourceType type, int amount)
	{
		data[type] = amount;
		Changed?.Invoke();
	}

	public void Add(Resource.ResourceType type, int amount)
	{
		data[type] += amount;
		Changed?.Invoke();
	}

	public bool Remove(Resource.ResourceType type, int amount)
	{
		if (data[type] < amount)
			return false;

		data[type] -= amount;
		Changed?.Invoke();
		return true;
	}

	public int Get(Resource.ResourceType type) => data[type];

	public IReadOnlyDictionary<Resource.ResourceType, int> All => data;
}
