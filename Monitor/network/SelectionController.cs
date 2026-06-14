using Godot;
using zappy;

// Maneja la selección de entidades por click (raycast desde Camera) y la
// actualización del InventoryPanel correspondiente. Extraído de Connection
// para mantenerla delgada (HandleLeftClick / ShowInventory / PlayerClicked).
public class SelectionController
{
    private readonly Terrain _terrainManager;
    private readonly InventoryPanel _inventoryPanel;

    private ISelectable _selection;

    public SelectionController(Terrain terrainManager, InventoryPanel inventoryPanel)
    {
        _terrainManager = terrainManager;
        _inventoryPanel = inventoryPanel;
    }

    public void HandleLeftClick(GodotObject collider, Vector3 position)
    {
        Log.Debug($"Entra en HandleLeftClick: {position}");

        Node node = collider as Node;

        while (node != null)
        {
            if (node is Player player)
            {
                Log.Debug("Colisiona con Player");
                _terrainManager.DeselectTile();
                PlayerClicked(player);
                return;
            }

            if (node is Resource)
            {
                Log.Debug("Colisiona con Recurso");

                // Clicar un recurso muestra el inventario de la casilla que lo contiene.
                Tile resourceTile = _terrainManager.GetTileFromPosition(position);
                if (resourceTile != null)
                {
                    _terrainManager.SelectTile(resourceTile.Coord.X, resourceTile.Coord.Y);
                    ShowInventory(resourceTile);
                }
                else
                {
                    _terrainManager.DeselectTile();
                }
                return;
            }

            node = node.GetParent();
        }

        // Si no es entidad → terreno
        Log.Debug("Colisiona con Terreno");

        Tile tile = _terrainManager.GetTileFromPosition(position);

        if (tile != null)
        {
            _terrainManager.SelectTile(tile.Coord.X, tile.Coord.Y);
            ShowInventory(tile);
        }
        else
        {
            _terrainManager.DeselectTile();
        }
    }

    public void ShowInventory(object owner)
    {
        _selection?.UnHightlight();

        if (owner is ISelectable selectable)
        {
            _selection = selectable;
            selectable.Highlight();
        }
        else
        {
            _selection = null;
        }

        if (owner is IInventory inventoryOwner)
        {
            _inventoryPanel.ShowForTile(inventoryOwner);
        }
    }

    private void PlayerClicked(Player player)
    {
        ShowInventory(player);
    }
}
