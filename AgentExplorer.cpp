#include "AgentExplorer.h"
#include "Bid.h"

double GetVoirScore(Blackboard& bb) {
	double voirWeigth = 42;
	double bias = 100;
	double voirScore = 0;

	std::vector<std::pair<int, int>> offsets = bb.GetVoirOffsets(bb.Level, bb.PlayerDirection);
	Tile* origin = bb.GetPlayerTile();

	for (size_t i = 0; i < offsets.size(); ++i) {
		std::pair<int, int> pair = offsets[i];
		int dx = pair.first;
		int dy = pair.second;
		Tile* tile = bb.Map.GetTile(origin->X + dx, origin->Y + dy);
		if (!tile)
			continue;

		voirScore += tile->GetExplorationValue(bb.CurrentTick) * voirWeigth;
	}

	return (bias + voirScore) * (1 - bb.GetHungerNeed());
}

void AgentExplorer::GetBids(Blackboard& bb)
{
	bb.Bids.push_back(Bid("gauche"));
	bb.Bids.push_back(Bid("droite"));
	bb.Bids.push_back(Bid("avance"));
	bb.Bids.push_back(Bid("voir", GetVoirScore(bb)));
}
