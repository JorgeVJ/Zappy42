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
	: connection(nullptr)
	, blackboard(nullptr)
{
}

ClientGame::~ClientGame()
{
	if (connection)
	{
		delete connection;
		connection = nullptr;
	}

	if (blackboard)
	{
		delete blackboard;
		blackboard = nullptr;
	}
}
