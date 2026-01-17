#include "AgentHungry.h"
#include "Bid.h"

double CalculateHunger(Blackboard& blackboard)
{
	double bias = 400;
	return bias * blackboard.GetHungerNeed();
}

void AgentHungry::GetBids(Blackboard& blackboard)
{
	double score = CalculateHunger(blackboard);
	Bid bid("prend nourriture", score);

	blackboard.Bids.push_back(bid);
}
