#pragma once
#include <vector>
#include "Bid.h"
#include "Inventory.h"
#include "Tile.h"
#include "Point.h"
#include <memory>
#include "Map.h"
#include "InfluenceService.h"
#include "ExplorationService.h"

/// <summary>
/// All the information needed to make decisions.
/// </summary>
class Blackboard
{
	public:
		Map Map;
		int Level = 1;
		Point Position;
		int CurrentTick;
		Inventory Inventory;
		std::vector<Bid> Bids;
		Direction PlayerDirection;
		std::vector<std::string> Messages;
		InfluenceService InfluenceService;
		ExplorationService ExplorationService;

		Blackboard(int x, int y);

		double GetHungerNeed();
		std::vector<std::pair<int, int>> GetVoirOffsets(int level, Direction dir);
		void PropagateInfluences(Tile* tile);
		Tile* GetPlayerTile();
		void HandleVoirResponse(const std::string& response);
};
