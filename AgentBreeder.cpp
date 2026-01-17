#include "AgentBreeder.h"
#include "Bid.h"

void AgentBreeder::GetBids(Blackboard& bb)
{
	double voirWeigth = 1;
	double bias = 100;
	double voirScore = bias + 5 * voirWeigth;

	double score = voirScore * (1 - bb.GetHungerNeed());

	bb.Bids.push_back(Bid("fork", score));
}
