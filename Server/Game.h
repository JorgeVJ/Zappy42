#pragma once
#include <vector>
#include "Connection.h"
#include "Map.h"
#include "EggData.h"
#include "TileDataRegistry.h"

class Game
{
	public:
		Map* WorldMap;
		TileDataRegistry<EggData> EggRegistry;
		std::vector<Connection*> Monitors;
		std::vector<Connection*> Players;

		static Game* GetInstance();

	private:
		Game();

		static Game* instance;
};

