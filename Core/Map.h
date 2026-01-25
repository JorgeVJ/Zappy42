#pragma once
#include <vector>
#include <memory>
#include "Tile.h"

class Map {
    public:
        Map(int width, int height);

        Tile* GetTile(int x, int y);

    private:
        int width;
        int height;
        std::vector<std::vector<std::unique_ptr<Tile>>> tiles;

        void CreateTiles();
        void ConnectTiles();
};

