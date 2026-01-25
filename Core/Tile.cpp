#include "pch.h"
#include "Tile.h"

Tile::Tile(int x, int y) : neighbors(8, nullptr), Point(x, y) {}

void Tile::SetNeighbor(Direction dir, Tile* tile) {
    neighbors[static_cast<int>(dir)] = tile;
}

Tile* Tile::GetNeighbor(Direction dir) const {
    return neighbors[static_cast<int>(dir)];
}
