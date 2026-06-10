using Godot;
using zappy;

public class Tile : IInventory
{
    public Vector2I Coord;
    public Inventory Inventory { get; } = new Inventory();

    public string DisplayTitle => $"Casilla ({Coord.X}, {Coord.Y})";

    public Tile(int x, int y)
    {
        Coord = new Vector2I(x, y);
    }
}
