#include "Map.h"
#include <queue>
#include <unordered_set>

Map::Map(int width, int height)
    : width(width), height(height)
{
    CreateTiles();
    ConnectTiles();
}

Tile* Map::GetTile(int x, int y) {
    int wrappedX = (x % width + width) % width;
    int wrappedY = (y % height + height) % height;
    return tiles[wrappedY][wrappedX].get();
}

void Map::CreateTiles() {
    tiles.resize(height);
    for (int y = 0; y < height; ++y) {
        tiles[y].resize(width);
        for (int x = 0; x < width; ++x) {
            tiles[y][x] = std::make_unique<Tile>(x, y);
        }
    }
}

void Map::ConnectTiles() {
    for (int y = 0; y < height; ++y) {
        for (int x = 0; x < width; ++x) {
            Tile* t = GetTile(x, y);
            if (!t) continue;

            t->SetNeighbor(Direction::North, GetTile(x, y - 1));
            t->SetNeighbor(Direction::South, GetTile(x, y + 1));
            t->SetNeighbor(Direction::West, GetTile(x - 1, y));
            t->SetNeighbor(Direction::East, GetTile(x + 1, y));

            t->SetNeighbor(Direction::NorthWest, GetTile(x - 1, y - 1));
            t->SetNeighbor(Direction::NorthEast, GetTile(x + 1, y - 1));
            t->SetNeighbor(Direction::SouthWest, GetTile(x - 1, y + 1));
            t->SetNeighbor(Direction::SouthEast, GetTile(x + 1, y + 1));
        }
    }
}

void Propagate(Tile* tile, std::queue<std::pair<int, Tile*>>& queue, int steps)
{
    if (tile)
    {
        queue.push({ steps, tile });
    }
}

void Map::BFSPropagate(Tile* origin, Resource resource, Influence* influence, int maxSteps)
{
    using Node = std::pair<int, Tile*>;
    std::queue<Node> q;

    q.push({ 0, origin });

    while (!q.empty()) {
        Tile* tile = q.front().second;
        int steps = q.front().first;
        q.pop();

        if (steps > maxSteps)
            continue;

        std::vector<std::pair<int, Influence*>>& vec = tile->GetInfluences(resource);

        // ¿Ya existe esta influencia con menos o igual steps?
        bool shouldAdd = true;
        for (std::pair<int, Influence*> data : vec) {
            if (data.second == influence) {
                shouldAdd = false;
                break;
            }
        }

        if (!shouldAdd)
            continue;

        // Añadir o actualizar
        vec.emplace_back(steps, influence);

        // Propagar a vecinos
        Propagate(tile->GetNeighbor(Direction::North), q, steps + 1);
        Propagate(tile->GetNeighbor(Direction::West), q, steps + 1);
        Propagate(tile->GetNeighbor(Direction::South), q, steps + 1);
        Propagate(tile->GetNeighbor(Direction::East), q, steps + 1);
    }
}
