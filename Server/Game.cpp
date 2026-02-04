#include "Game.h"
#include <random>
#include <iostream>
//hola
/* Hola */

Game* Game::instance = nullptr;

Game* Game::GetInstance()
{
    if (instance == nullptr)
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
            tile->inventory.Add(Resource::Food, dist(gen));
            tile->inventory.Add(Resource::Linemate, dist(gen));
            tile->inventory.Add(Resource::Deraumere, dist(gen));
            tile->inventory.Add(Resource::Sibur, dist(gen));
            tile->inventory.Add(Resource::Mendiane, dist(gen));
            tile->inventory.Add(Resource::Phiras, dist(gen));
            tile->inventory.Add(Resource::Thystame, dist(gen));
        }
    }
}
