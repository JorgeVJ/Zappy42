#pragma once
#include "Connection.h"
#include "Blackboard.h"

class ClientGame
{
public:
	Connection* Connection;
	Blackboard* Blackboard;

	static ClientGame* GetInstance();
	static void Dispose();

	~ClientGame();

private:
	ClientGame();

	static ClientGame* instance;
};

