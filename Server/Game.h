#pragma once
#include <vector>
#include "Connection.h"
#include "Map.h"
#include "EggData.h"
#include "TileDataRegistry.h"
#include "TeamManager.h"

class Game
{
  public:
    Map* WorldMap;
    TileDataRegistry<EggData> EggRegistry;

    TeamManager              teamsManager;
    std::vector<Connection*> Monitors;
    std::vector<Connection*> Players;
    // Should create a connection Manager for history 10 last commands
    //TileManager that got TileDataRegistry of <Player*> and <EggData>
    static Game* SetInstance(int x, int y);
    static Game* GetInstance();

	private:
    Game(int x, int y);

		static Game* instance;
};
