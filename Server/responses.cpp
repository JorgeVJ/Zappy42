#include <string>
#include "Tile.h"
#include "Game.h"

std::string GetTileBCT(int x, int y)
{
    Game* game = Game::GetInstance();

    Tile* tile = game->WorldMap->GetTile(x, y);
    if (!tile) return "";

    std::stringstream ss;
    ss << "bct " << x << " " << y;

    for (size_t i = 0; i < Inventory::Size(); ++i) {
        ss << " " << tile->inventory.Get(static_cast<Resource>(i));
    }

    ss << "\n";
    return ss.str();
}

std::string GetMCT()
{
    Game* game = Game::GetInstance();
    std::stringstream ss;

    for (int y = 0; y < game->WorldMap->Height; ++y) {
        for (int x = 0; x < game->WorldMap->Width; ++x) {
            ss << GetTileBCT(x, y);
        }
    }

    return ss.str();
}
