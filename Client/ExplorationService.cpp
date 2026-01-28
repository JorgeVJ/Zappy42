#include "ExplorationService.h"

void ExplorationService::MarkSeen(Tile* tile, int currentTick) {
    auto* data = Registry.TryGet(tile);
    if (!data)
    {
        auto newData = ExplorationData();
        newData.LastSeenTick = currentTick;
        Registry.Add(tile, newData);
    }
    else
    {
        data->LastSeenTick = currentTick;
    }
}

double ExplorationService::GetExplorationValue(Tile* tile, int currentTick, int decayTicks) const {
    auto* data = Registry.TryGet(tile);
    if (!data)
        return 1;

    int delta = currentTick - data->LastSeenTick;
    double value = static_cast<double>(delta) / decayTicks;
    if (value < 0.0) value = 0.0;
    if (value > 1.0) value = 1.0;
    return value;
}
