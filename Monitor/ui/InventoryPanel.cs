using Godot;
using zappy;

public partial class InventoryPanel : Control
{
	[Export] private Label title;
	[Export] private Label food;
	[Export] private Label linemate;
	[Export] private Label deraumere;
	[Export] private Label sibur;
	[Export] private Label mendiane;
	[Export] private Label phiras;
	[Export] private Label thystame;
	[Export] private Label titleDetail;
	[Export] private Label foodDetail;
	[Export] private Label linemateDetail;
	[Export] private Label deraumereDetail;
	[Export] private Label siburDetail;
	[Export] private Label mendianeDetail;
	[Export] private Label phirasDetail;
	[Export] private Label thystameDetail;

	public override void _Ready()
	{
		Hide();
	}

	public void ShowForTile(IInventory owner)
	{
		if (owner == null)
		{
			GD.Print("Owner is null");
			return;
		}
		// title.Text = $"Tile ({tile.GridPos.X}, {tile.GridPos.Y})";

		food.Text = $"Food:";
		foodDetail.Text = $"{owner.Inventory.Get(Resource.ResourceType.Nourriture)}";
		linemate.Text = $"Linemate:";
		linemateDetail.Text = $"{owner.Inventory.Get(Resource.ResourceType.Linemate)}";
		deraumere.Text = $"Deraumere:";
		deraumereDetail.Text = $"{owner.Inventory.Get(Resource.ResourceType.Deraumere)}";
		sibur.Text = $"Sibur:";
		siburDetail.Text = $"{owner.Inventory.Get(Resource.ResourceType.Sibur)}";
		mendiane.Text = $"Mendiane:";
		mendianeDetail.Text = $"{owner.Inventory.Get(Resource.ResourceType.Mendiane)}";
		phiras.Text = $"Phiras:";
		phirasDetail.Text = $"{owner.Inventory.Get(Resource.ResourceType.Phiras)}";
		thystame.Text = $"Thystame:";
		thystameDetail.Text = $"{owner.Inventory.Get(Resource.ResourceType.Thystame)}";

		Show();
	}

	public void HidePanel() => Hide();
}
