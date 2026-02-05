#pragma once
#include "Connection.h"
#include "Blackboard.h"

class ClientGame
{
public:
	Connection* connection;
	Blackboard* blackboard;

	static ClientGame* GetInstance();
	static void Dispose();

	~ClientGame();

private:
	ClientGame();

	static ClientGame* instance;
};
