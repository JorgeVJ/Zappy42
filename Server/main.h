#pragma once
#include "Game.h"
#include "Connection.h"

void HandlePlayerConnection(Game* game, Connection& client, const std::string& cmd);
