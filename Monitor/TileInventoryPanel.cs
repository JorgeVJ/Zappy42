using Godot;
using System;

public partial class TileInventoryPanel : Control
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

    public void ShowForTile(Tile tile)
    {
        if (tile == null)
            GD.Print("Tile is null");
        // title.Text = $"Tile ({tile.GridPos.X}, {tile.GridPos.Y})";

        food.Text = $"Food:";
        foodDetail.Text = $"{tile.GetResource(Resource.ResourceType.Nourriture)}";
        linemate.Text = $"Linemate:";
        linemateDetail.Text = $"{tile.GetResource(Resource.ResourceType.Linemate)}";
        deraumere.Text = $"Deraumere:";
        deraumereDetail.Text = $"{tile.GetResource(Resource.ResourceType.Deraumere)}";
        sibur.Text = $"Sibur:";
        siburDetail.Text = $"{tile.GetResource(Resource.ResourceType.Sibur)}";
        mendiane.Text = $"Mendiane:";
        mendianeDetail.Text = $"{tile.GetResource(Resource.ResourceType.Mendiane)}";
        phiras.Text = $"Phiras:";
        phirasDetail.Text = $"{tile.GetResource(Resource.ResourceType.Phiras)}";
        thystame.Text = $"Thystame:";
        thystameDetail.Text = $"{tile.GetResource(Resource.ResourceType.Thystame)}";

        Show();
    }

    public void HidePanel() => Hide();
}
