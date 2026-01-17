#pragma once
#include <vector>
#include "Bid.h"
#include "Inventory.h"
#include "Tile.h"
#include "Point.h"
#include <memory>
#include "Map.h"

/// <summary>
/// All the information needed to make decisions.
/// </summary>
class Blackboard
{
	public:
		int Level = 1;
		Point Position;
		Direction Direction;
		Map Map;
		Inventory Inventory;
		std::vector<Bid> Bids;
		std::vector<std::string> Messages;

		Blackboard(int x, int y);

		double GetHungerNeed();
		void PropagateInfluences(Tile* tile);
		Tile* GetPlayerTile();
		void HandleVoirResponse(const std::string& response);
};
