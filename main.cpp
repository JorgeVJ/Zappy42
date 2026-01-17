#include "Blackboard.h"
#include "IAgent.h"
#include "AgentExplorer.h"
#include "AgentHungry.h"
#include "AgentChaman.h"
#include "AgentBreeder.h"
#include "AgentStoner.h"
#include <iostream>

Bid* GetBestBid(std::vector<Bid>& bids)
{
	if (bids.empty())
		return nullptr;

	Bid* best = &bids[0];
	for (auto& bid : bids) {
		if (bid.Value > best->Value)
			best = &bid;
	}
	return best;
}

int main()
{
	Blackboard board(5, 5);
	std::vector<IAgent*> agents;
	agents.push_back(new AgentExplorer());
	agents.push_back(new AgentHungry());
	agents.push_back(new AgentChaman());
	agents.push_back(new AgentBreeder());
	agents.push_back(new AgentStoner());
	
	Bid* lastBid = nullptr;

	while (true)
	{
		board.Bids.clear();

		for (auto& agent : agents) {
			agent->GetBids(board);
		}

		Bid* bestBid = GetBestBid(board.Bids);
		if (!bestBid) {
			std::cout << "No hay bids disponibles\n";
			continue;
		}

		if (!lastBid)
		{
			lastBid = bestBid;
			continue;
		}

		std::string inventoryResponse = "{nourriture 345, sibur 3, phiras 5, deraumere 0}";
		std::string voirResponse = "{nourriture, sibur, phiras phiras }";

		if (lastBid->Command == "inventory" && inventoryResponse.c_str()[0] == '{') {
			board.Inventory.SetFromServerString(inventoryResponse);
		}
		else if (lastBid->Command == "voir" && voirResponse.c_str()[0] == '{') {
			board.HandleVoirResponse(voirResponse);
		}

		lastBid = bestBid;
	}

	return 0;
}