#pragma once
#include <vector>
#include <memory>
#include "Bid.h"
#include "Tile.h"

class Map {
    public:
        Map(int width, int height);
        Tile* GetTile(int x, int y);
        void BFSPropagate(Tile* origin, Resource resource, Influence* influence, int maxSteps);

    private:
        int width;
        int height;
        std::vector<std::vector<std::unique_ptr<Tile>>> tiles;

        void CreateTiles();
        void ConnectTiles();
};

