#include "pch.h"
#include "Tile.h"

Tile::Tile(int x, int y) : Point(x, y), neighbors(8, nullptr) {}

void Tile::SetNeighbor(Direction dir, Tile* tile) {
    neighbors[static_cast<int>(dir)] = tile;
}

Tile* Tile::GetNeighbor(Direction dir) const {
    return neighbors[static_cast<int>(dir)];
}
