#pragma once
#include "Map.h"

class Game
{
	public:
		Map* WorldMap;

		static Game* GetInstance();

	private:
		Game();

		static Game* instance;
};

