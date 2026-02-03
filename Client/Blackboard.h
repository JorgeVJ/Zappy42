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
#include <CommandHistory.h>
#include "Connection.h"

/// <summary>
/// All the information needed to make decisions.
/// </summary>
class Blackboard
{
	public:
		Map Map;
		int CurrentTick;
		Player Me;
		std::vector<Bid> Bids;
		std::vector<std::string> Messages;
		InfluenceService InfluenceService;
		ExplorationService ExplorationService;
		CommandHistory commandHistory;
		Connection* Sock;
		std::string teamName;

		Blackboard(Connection* connection);

		void InitializeMap(int x, int y);
		void setTeamName(const std::string& name);
		double GetHungerNeed();
		std::vector<std::pair<int, int>> GetVoirOffsets(int level, Direction dir);
		void PropagateInfluences(Tile* tile);
		Tile* GetPlayerTile();
		void HandleVoirResponse(const std::string& response);
};
