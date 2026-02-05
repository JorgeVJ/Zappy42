#pragma once
#include "Map.h"
#include "TileDataRegistry.h"
#include <vector>
#include <map>

struct Influence {
    bool IsActive;
    Resource resource;
    Tile* Origin;
};

struct TileInfluenceState
{
    /// <summary>
    /// Al propagarse una señal desde un Tile, se mantiene un registro de las señales enviadas desde cada Tile.
    /// </summary>
    std::vector<Influence*> Signals;

    /// <summary>
    /// Al propagarse una señal esta va dejando un registro de Influencia en influences, se guarda un registro distinto para cada recurso.
    /// </summary>
    std::array<std::vector<std::pair<int, Influence*>>, (size_t)Resource::Count> Influences;
};

class InfluenceService
{
    public:
        TileDataRegistry<TileInfluenceState> Registry;

        void BFSPropagate(Tile* origin, Resource resource, Influence* influence, int maxSteps);
        void CleanSignals(Tile* tile);
        double GetInfluenceStrength(Tile* tile, Resource resource);
        std::vector<std::pair<int, Influence*>>& GetInfluences(Tile* tile, Resource resource);
};
