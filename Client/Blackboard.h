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
#include "MessageEntry.h"

/// <summary>
/// All the information needed to make decisions.
/// </summary>
class Blackboard
{
	public:
		/// <summary>
		/// Sum of all Command Ticks.
		/// </summary>
		int CurrentTick;

		Map Map;
		Player Me;

		/// <summary>
		/// Contiene las pujas de los agentes para el tick actual. Se limpia cada tick despues de elegir la mejor puja.
		/// </summary>
		std::vector<Bid> Bids;

		/// <summary>
		/// Contain the Broadcasts from other players.
		/// </summary>
		std::vector<MessageEntry> Messages;
		
		/// <summary>
		/// Servicio para manejar las influencias de los recursos en el mapa. 
		/// </summary>
		InfluenceService InfluenceService;
		ExplorationService ExplorationService;

		CommandHistory commandHistory;

		Blackboard(); // constructor por defecto

		void InitializeMap(int x, int y);
		double GetHungerNeed();
		std::vector<std::pair<int, int>> GetVoirOffsets(int level, Direction dir);
		void PropagateInfluences(Tile* tile);
		Tile* GetPlayerTile();
		void HandleVoirResponse(const std::string& response);
};
