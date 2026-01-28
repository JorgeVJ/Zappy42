#pragma once
#include "TileDataRegistry.h"

struct ExplorationData {
    public:
        int LastSeenTick = -1;
};

class ExplorationService
{
    public:
        TileDataRegistry<ExplorationData> Registry;
    
        void MarkSeen(Tile *tile, int currentTick);
        double GetExplorationValue(Tile *tile, int currentTick, int decayTicks = 50) const;
};

