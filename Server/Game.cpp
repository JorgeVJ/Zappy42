#include "Game.h"
#include <random>

Game* Game::instance = nullptr;

Game* Game::GetInstance()
{
    if (!instance)
        instance = new Game();

    return instance;
}

Game::Game()
{
    this->WorldMap = new Map(8, 8);
    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_int_distribution<> dist(0, 2);

    // Recursos aleatorios
    for (int y = 0; y < this->WorldMap->Height; ++y) {
        for (int x = 0; x < this->WorldMap->Width; ++x) {
            Tile* tile = this->WorldMap->GetTile(x, y);
            if (!tile) continue;

            tile->Inventory.Add(Resource::Food, dist(gen));
            tile->Inventory.Add(Resource::Linemate, dist(gen));
            tile->Inventory.Add(Resource::Deraumere, dist(gen));
            tile->Inventory.Add(Resource::Sibur, dist(gen));
            tile->Inventory.Add(Resource::Mendiane, dist(gen));
            tile->Inventory.Add(Resource::Phiras, dist(gen));
            tile->Inventory.Add(Resource::Thystame, dist(gen));
        }
    }
}
