#include "ClientGame.h"

ClientGame* ClientGame::instance = nullptr;

ClientGame* ClientGame::GetInstance()
{
	if (!instance)
		instance = new ClientGame();

	return instance;
}

void ClientGame::Dispose()
{
	delete instance;
	instance = nullptr;
}

ClientGame::ClientGame()
	: Connection(nullptr)
	, Blackboard(nullptr)
{
}

ClientGame::~ClientGame()
{
	if (Connection)
	{
		delete Connection;
		Connection = nullptr;
	}

	if (Blackboard)
	{
		delete Blackboard;
		Blackboard = nullptr;
	}
}
