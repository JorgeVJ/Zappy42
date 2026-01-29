#include "events.h"

/// <summary>
/// New player connection
/// </summary>
/// <param name="client"></param>
void pnw(Connection* connection, Connection* monitor)
{
	if (!monitor->IsMonitor() || !connection->IsPlayer())
		return;

	std::ostringstream ss;
	ss << "pnw " << connection->Player->ID << " "
		<< connection->Player->Position.X << " "
		<< connection->Player->Position.Y << " "
		<< (int)connection->Player->Orientation << " "
		<< connection->Player->Level << " "
		<< connection->Player->TeamName;
	monitor->SendLine(ss.str());
}

/// <summary>
/// The egg is laid on the tile by a player
/// </summary>
/// <param name="client"></param>
void enw(EggData* egg, Connection* monitor)
{

}