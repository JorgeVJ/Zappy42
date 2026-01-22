#pragma once
#include "Event.h"
#include <string>
#include <algorithm>
#include <array>
#include <vector>
#include "Inventory.h"
#include "Point.h"
#include "Direction.h"

class Tile;

struct Influence {
    bool IsActive;
    Resource Resource;
    Tile* Origin;
};

class Tile : public Point {
    public:
        bool EverSeen = false;
        Inventory Inventory;
        std::vector<Influence*> Signals;

        Tile(int x, int y);

        void SetNeighbor(Direction dir, Tile* tile);
        Tile* GetNeighbor(Direction dir) const;
        void MarkSeen(int currentTick);
        double GetExplorationValue(int currentTick, int decayTicks = 50) const;
        double GetInfluenceStrength(Resource resource);
        std::vector<std::pair<int, Influence*>>& GetInfluences(Resource resource);
        void CleanInfluences();

    private:
        int lastSeenTick = -1;
        std::vector<Tile*> neighbors;
        std::array<std::vector<std::pair<int, Influence *>>, static_cast<size_t>(Resource::Count)> influences;
};