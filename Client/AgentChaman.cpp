#include "AgentChaman.h"
#include "Bid.h"
#include "CommandEntry.h"
#include "ClientLog.h"

void AgentChaman::GetBids(Blackboard& bb)
{
	// Verificar si puede hacer incantacion
	if (bb.Me.Level < 8)
	{
		auto it = Inventory::IncantationRecipes.find(bb.Me.Level);
		if (it != Inventory::IncantationRecipes.end())
		{
			const IncantationRecipe& recipe = it->second;

			// Verificar si tiene los recursos necesarios
			if (bb.Me.inventory.Has(recipe.RequiredResources))
			{
				// TODO: Verificar tambien si hay suficientes jugadores en el tile
				LOG_CHAMAN("Can perform incantation to level " << (bb.Me.Level + 1));
				LOG_CHAMAN("Required players: " << recipe.RequiredPlayers);

				bb.Bids.push_back(Bid(
					CommandEntry::Create(CommandType::Incantation, bb.CurrentTick),
					200.0
				));
			}
			else
			{
				LOG_CHAMAN("Missing resources for level " << (bb.Me.Level + 1));
			}
		}
	}
	
	// Broadcast para coordinar
	// Change value or later behavior
	//bb.Bids.push_back(Bid(
	//	CommandEntry::Create(CommandType::Broadcast, "Marco", bb.CurrentTick),
	//	20.0
	//));
}

AgentChaman::~AgentChaman() {};
