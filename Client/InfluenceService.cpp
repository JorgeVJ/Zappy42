#include "InfluenceService.h"
#include <queue>

void Propagate(Tile* tile, std::queue<std::pair<int, Tile*>>& queue, int steps)
{
    if (tile)
    {
        queue.push({ steps, tile });
    }
}

void InfluenceService::BFSPropagate(Tile* origin, Resource resource, Influence* influence, int maxSteps)
{
    using Node = std::pair<int, Tile*>;
    std::queue<Node> q;
    TileInfluenceState* data = Registry.TryGet(origin);
    if (data)
        data->Signals.push_back(influence);
    else
    {
        TileInfluenceState state;
        state.Signals.push_back(influence);
        Registry.Add(origin, state);
    }

    q.push({ 0, origin });

    while (!q.empty()) {
        Tile* tile = q.front().second;
        int steps = q.front().first;
        q.pop();

        if (steps > maxSteps)
            continue;

        std::vector<std::pair<int, Influence*>>& vec = GetInfluences(tile, resource);

        // ¿Ya existe esta influencia con menos o igual steps?
        bool shouldAdd = true;
        for (std::pair<int, Influence*> data : vec) {
            if (data.second == influence) {
                shouldAdd = false;
                break;
            }
        }

        if (!shouldAdd)
            continue;

        // Añadir o actualizar
        vec.emplace_back(steps, influence);

        // Propagar a vecinos
        Propagate(tile->GetNeighbor(Direction::North), q, steps + 1);
        Propagate(tile->GetNeighbor(Direction::West), q, steps + 1);
        Propagate(tile->GetNeighbor(Direction::South), q, steps + 1);
        Propagate(tile->GetNeighbor(Direction::East), q, steps + 1);
    }
}

void InfluenceService::CleanSignals(Tile* tile)
{
    auto* data = Registry.TryGet(tile);
    if (!data)
        return;

    std::vector<Influence*>& signals = data->Signals;
    for (auto& influence : signals)
    {
        influence->IsActive = false;
    }
    signals.clear();
}

double InfluenceService::GetInfluenceStrength(Tile* tile, Resource resource)
{
    auto* data = Registry.TryGet(tile);
    if (!data)
        return 0;

    auto& vec = data->Influences[static_cast<size_t>(resource)];
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

std::vector<std::pair<int, Influence*>>& InfluenceService::GetInfluences(Tile* tile, Resource resource)
{
    auto& vec = Registry.Get(tile)->Influences[static_cast<size_t>(resource)];

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
