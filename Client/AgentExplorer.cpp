#include "AgentExplorer.h"
#include "Bid.h"
#include "CommandEntry.h"

double GetVoirScore(Blackboard& bb) {
	double voirWeigth = 42;
	double bias = 100;
	double voirScore = 0;

	std::vector<std::pair<int, int>> offsets = bb.GetVoirOffsets(bb.Me.Level, bb.Me.Orientation);
	Tile* origin = bb.GetPlayerTile();

	for (size_t i = 0; i < offsets.size(); ++i) {
		std::pair<int, int> pair = offsets[i];
		int dx = pair.first;
		int dy = pair.second;
		Tile* tile = bb.map.GetTile(origin->X + dx, origin->Y + dy);
		if (!tile)
			continue;

		voirScore += bb.explorationService.GetExplorationValue(tile, bb.CurrentTick * voirWeigth);
	}

	return (bias + voirScore) * (1 - bb.GetHungerNeed());
}

void AgentExplorer::GetBids(Blackboard& bb)
{
	int score = 12;
	bb.Bids.push_back(Bid(CommandEntry::Create(CommandType::Left, bb.CurrentTick), score));
	bb.Bids.push_back(Bid(CommandEntry::Create(CommandType::Right, bb.CurrentTick), score));
	bb.Bids.push_back(Bid(CommandEntry::Create(CommandType::Advance, bb.CurrentTick), score));
	bb.Bids.push_back(Bid(CommandEntry::Create(CommandType::See, bb.CurrentTick), GetVoirScore(bb)));
}
AgentExplorer::~AgentExplorer() {};
