#include "pch.h"
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
