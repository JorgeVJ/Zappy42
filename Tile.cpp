#include "Tile.h"

Tile::Tile(int x, int y) : neighbors(8, nullptr), Point(x, y) {}

void Tile::SetNeighbor(Direction dir, Tile* tile) {
    neighbors[static_cast<int>(dir)] = tile;
}

Tile* Tile::GetNeighbor(Direction dir) const {
    return neighbors[static_cast<int>(dir)];
}

void Tile::MarkSeen(int currentTick) {
    lastSeenTick = currentTick;
    EverSeen = true;
}

double Tile::GetExplorationValue(int currentTick, int decayTicks) const {
    if (!EverSeen)
        return 1;

    int delta = currentTick - lastSeenTick;
    double value = static_cast<double>(delta) / decayTicks;
    if (value < 0.0) value = 0.0;
    if (value > 1.0) value = 1.0;
    return value;
}

double Tile::GetInfluenceStrength(Resource resource)
{
    auto& vec = influences[static_cast<size_t>(resource)];
    double total = 0.0;

    // Limpieza lazy + acumulación
    vec.erase(
        std::remove_if(vec.begin(), vec.end(),
            [&](const std::pair<int, Influence*>& inst) {
                if (!inst.second->IsActive)
                    return true;

                total += inst.first;
                return false;
            }),
        vec.end()
    );

    return total;
}

std::vector<std::pair<int, Influence*>>& Tile::GetInfluences(Resource resource)
{
    auto& vec = influences[static_cast<size_t>(resource)];

    // Limpieza lazy
    vec.erase(
        std::remove_if(vec.begin(), vec.end(),
            [&](const std::pair<int, Influence*>& inst) {
                return !inst.second->IsActive;
            }),
        vec.end()
    );

    return vec;
}

void Tile::CleanInfluences()
{
    for (auto& signal : Signals)
    {
        signal->IsActive = false;
    }
    Signals.clear();
}
