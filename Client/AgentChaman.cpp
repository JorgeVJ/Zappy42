#include "AgentChaman.h"
#include "Bid.h"
#include "CommandEntry.h"
#include <iostream>

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
				std::cout << "[Chaman] Can perform incantation to level " << (bb.Me.Level + 1) << "\n";
				std::cout << "[Chaman] Required players: " << recipe.RequiredPlayers << "\n";
				
				bb.Bids.push_back(Bid(
					CommandEntry::Create(CommandType::Incantation, bb.CurrentTick),
					200.0
				));
			}
			else
			{
				std::cout << "[Chaman] Missing resources for level " << (bb.Me.Level + 1) << "\n";
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
