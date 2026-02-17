#include <iostream>
#include "AgentStoner.h"
#include "Bid.h"
#include "CommandEntry.h"
#include "Inventory.h"

void AgentStoner::GetBids(Blackboard& bb)
{
	Tile* playerTile = bb.GetPlayerTile();
	if (!playerTile)
		return;
	
	// Obtener la receta para el nivel actual
	auto it = Inventory::IncantationRecipes.find(bb.Me.Level);
	if (it == Inventory::IncantationRecipes.end())
	{
		std::cout << "[Stoner] No recipe found for level " << bb.Me.Level << "\n";
		return;
	}
	
	IncantationRecipe& recipe = it->second;
	Inventory& required = recipe.RequiredResources;
	
	// Mapeo de recursos
	struct ResourceInfo {
		std::string name;
		Resource enumVal;
	};
	
	const ResourceInfo resources[] = {
		{"linemate", Resource::Linemate},
		{"deraumere", Resource::Deraumere},
		{"sibur", Resource::Sibur},
		{"mendiane", Resource::Mendiane},
		{"phiras", Resource::Phiras},
		{"thystame", Resource::Thystame}
	};
	
	std::cout << "[Stoner] Analyzing resources for level " << bb.Me.Level << " incantation\n";
	
	// FASE 1: RECOGER recursos prioritarios
	for (const auto& res : resources)
	{
		int onTile = playerTile->Inventory.Get(res.enumVal);
		if (onTile <= 0)
			continue; // No hay en el tile
		
		int needed = required.Get(res.enumVal);
		int current = bb.Me.Inventory.Get(res.enumVal);
		int missing = needed - current;
		
		double priority = 0.0;
		
		if (missing > 0)
		{
			// ALTA PRIORIDAD: Necesitamos este recurso para incantación
			priority = 80.0 + (missing * 15.0); // Rango: 80-125 Más prioritario cuanto más falte
			std::cout << "[Stoner] CRITICAL: Need " << missing << " more " << res.name 
			          << " (have " << current << "/" << needed << "). Priority: " << priority << "\n";
		}
		else if (needed > 0 && current < needed + 2)
		{
			// PRIORIDAD MEDIA: Tenemos lo justo, pero podemos recoger más como backup
			priority = 40.0;
			std::cout << "[Stoner] BACKUP: Collecting extra " << res.name 
			          << " (have " << current << ", need " << needed << "). Priority: " << priority << "\n";
		}
		else if (needed > 0)
		{
			// PRIORIDAD BAJA: Tenemos suficiente
			priority = 15.0;
			std::cout << "[Stoner] LOW: Have enough " << res.name 
			          << " (" << current << "/" << needed << "). Priority: " << priority << "\n";
		}
		else
		{
			// PRIORIDAD MUY BAJA: No lo necesitamos para incantación, pero puede ser útil después
			priority = 8.0;
			std::cout << "[Stoner] FUTURE: " << res.name 
			          << " not needed for current level. Priority: " << priority << "\n";
		}
		
		// Ajustar prioridad según hambre
		priority *= (1 - bb.GetHungerNeed());
		
		bb.Bids.push_back(Bid(
			CommandEntry::Create(CommandType::Take, res.name, bb.CurrentTick),
			priority
		));
	}
}
