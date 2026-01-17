#include "AgentExplorer.h"
#include "Bid.h"

void AgentExplorer::GetBids(Blackboard& bb)
{
	bb.Bids.push_back(Bid("gauche"));
	bb.Bids.push_back(Bid("droite"));
	bb.Bids.push_back(Bid("avance"));

	double voirWeigth = 1;
	double bias = 100;
	double voirScore = bias + 5 * voirWeigth;

	double score = voirScore * (1 - bb.GetHungerNeed());

	bb.Bids.push_back(Bid("voir", score + 500));
}
