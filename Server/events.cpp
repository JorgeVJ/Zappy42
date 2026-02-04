#include "events.h"
#include <iostream> // For placeholder
/// <summary>
/// New player connection
/// </summary>
/// <param name="client"></param>
void pnw(Connection* connection, Connection* monitor)
{
	if (!monitor->IsMonitor() || !connection->IsPlayer())
		return;

	std::ostringstream ss;
	ss << "pnw " << connection->player->ID << " "
		<< connection->player->Position.X << " "
		<< connection->player->Position.Y << " "
		<< (int)connection->player->Orientation << " "
		<< connection->player->Level << " "
		<< connection->player->TeamName;
	monitor->SendLine(ss.str());
}

/// <summary>
/// The egg is laid on the tile by a player
/// </summary>
/// <param name="client"></param>
void enw(EggData* egg, Connection* monitor)
{
    if  (egg && monitor)
		std::cout << egg << monitor << std::endl;
}
