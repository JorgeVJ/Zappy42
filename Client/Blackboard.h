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
#include <Player.h>

/// <summary>
/// All the information needed to make decisions.
/// </summary>
class Blackboard
{
	public:
		Map map;
		int CurrentTick;
		Player Me;
		std::vector<Bid> Bids;
		std::vector<std::string> Messages;
		InfluenceService influenceService;
		ExplorationService explorationService;

		Blackboard(int x, int y);

		double GetHungerNeed();
		std::vector<std::pair<int, int>> GetVoirOffsets(int level, Direction dir);
		void PropagateInfluences(Tile* tile);
		Tile* GetPlayerTile();
		void HandleVoirResponse(const std::string& response);
};
