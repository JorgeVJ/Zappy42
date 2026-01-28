#include "AgentStoner.h"
#include <map>

Inventory ComputeMaxNeeded()
{
    Inventory max;
    const std::map<int, Inventory>& recipes = Inventory::IncantationRecipes;

    for (std::pair<int, Inventory> pair : recipes)
    {
        for (size_t i = 0; i < Inventory::Size(); ++i)
        {
            Resource r = static_cast<Resource>(i);
            int needed = pair.second.Get(r);
            int currentMax = max.Get(r);

            if (needed > currentMax)
                max.Add(r, needed - currentMax);
        }
    }

    return max;
}

double ResourceNeedScore(
    Resource r,
    Inventory& have,
    Inventory& needNow,
    Inventory& maxNeed
)
{
    int current = have.Get(r);
    int needed = needNow.Get(r);
    int maxAllowed = maxNeed.Get(r);

    // Ya no sirve recoger más
    if (current >= maxAllowed)
        return 0.0;

    // Urgencia inmediata
    if (current < needed)
        return 2.0 + (needed - current);

    // Stock útil para futuros niveles
    return 0.5 * (maxAllowed - current) / maxAllowed;
}

void AgentStoner::GetBids(Blackboard& blackboard)
{
    Inventory tileInv = blackboard.GetPlayerTile()->Inventory;
    Inventory max = ComputeMaxNeeded();
    Inventory lvl = Inventory::IncantationRecipes[blackboard.Level];
    for (int i = 0; i < (int)Resource::Count; i++) {
        Resource resource = (Resource)i;
        int qtty = tileInv.Get(resource);
        if (qtty > 0) {
	        Bid bid("prend " + Inventory::ResourceToString(resource), ResourceNeedScore(resource, blackboard.Inventory, lvl, max));
        	blackboard.Bids.push_back(bid);
        }
    }
}
