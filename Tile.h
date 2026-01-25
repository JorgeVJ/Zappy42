#pragma once
#include "Event.h"
#include <string>
#include <algorithm>
#include <array>
#include <vector>
#include "Inventory.h"
#include "Point.h"
#include "Direction.h"

class Tile : public Point {
    public:
        Inventory Inventory;

        Tile(int x, int y);

        void SetNeighbor(Direction dir, Tile* tile);
        Tile* GetNeighbor(Direction dir) const;

    private:
        std::vector<Tile*> neighbors;
};