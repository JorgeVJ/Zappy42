#include "pch.h"
#include "Map.h"
#include <queue>
#include <unordered_set>

Map::Map(int width, int height)
    : Width(width), Height(height)
{
    CreateTiles();
    ConnectTiles();
}

Tile* Map::GetTile(int x, int y) {
    int wrappedX = (x % Width + Width) % Width;
    int wrappedY = (y % Height + Height) % Height;
    return tiles[wrappedY][wrappedX].get();
}

void Map::CreateTiles() {
    tiles.resize(Height);
    for (int y = 0; y < Height; ++y) {
        tiles[y].resize(Width);
        for (int x = 0; x < Width; ++x) {
            tiles[y][x] = std::make_unique<Tile>(x, y);
        }
    }
}

void Map::ConnectTiles() {
    for (int y = 0; y < Height; ++y) {
        for (int x = 0; x < Width; ++x) {
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
