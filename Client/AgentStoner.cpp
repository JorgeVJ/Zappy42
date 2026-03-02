#include <iostream>
#include <iostream>
#include "AgentStoner.h"
#include "Bid.h"
#include "CommandEntry.h"
#include "Inventory.h"

void AgentStoner::GetBids(Blackboard& bb)
{
	auto it = Inventory::IncantationRecipes.find(bb.Me.Level);
	if (it == Inventory::IncantationRecipes.end())
		return;

	const Inventory& required = it->second.RequiredResources;
	Tile* playerTile = bb.GetPlayerTile();
	if (!playerTile)
		return;

	std::cout << "[Stoner] Analyzing resources for level " << bb.Me.Level << " incantation\n";
	
	// FASE 1: SOLICITAR busqueda de recursos faltantes
	for (size_t r = 0; r < Inventory::Size(); ++r) {
		Resource res = static_cast<Resource>(r);
		if (res == Resource::Food) continue; // AgentHungry maneja la comida
		
		int needed = required.Get(res);
		int current = bb.Me.inventory.Get(res);
		
		if (current < needed) {
			int missing = needed - current;
			int priority = 80 + (missing * 5); // Mas falta = mas prioridad
			if (priority > 95) priority = 95; // Cap a 95 (comida critica tiene 100)
			
			bb.RequestResource(res, priority);
			std::cout << "[Stoner] Requesting search for: " 
			          << Inventory::ResourceToString(res) 
			          << " (need " << missing << " more) - Priority: " << priority << "\n";
		}
	}
	
	// FASE 2: Hacer bids para recoger recursos en el tile actual
	struct ResourceInfo {
		std::string name;
		Resource enumVal;
	};
	
	ResourceInfo resources[] = {
		{"linemate", Resource::Linemate},
		{"deraumere", Resource::Deraumere},
		{"sibur", Resource::Sibur},
		{"mendiane", Resource::Mendiane},
		{"phiras", Resource::Phiras},
		{"thystame", Resource::Thystame}
	};
	
	for (const auto& res : resources) {
		int onTile = playerTile->inventory.Get(res.enumVal);
		if (onTile <= 0)
			continue;
		
		int needed = required.Get(res.enumVal);
		int current = bb.Me.inventory.Get(res.enumVal);
		int missing = needed - current;
		
		double priority = 0.0;
		
		if (missing > 0) {
			priority = 80.0 + (missing * 10.0);
			std::cout << "[Stoner] HIGH PRIORITY: " << res.name 
			          << " needed! (missing: " << missing << ") - Priority: " << priority << "\n";
		}
		else if (missing == 0) {
			priority = 50.0;
			std::cout << "[Stoner] COMPLETE: " << res.name 
			          << " requirement met - Priority: " << priority << "\n";
		}
		else {
			priority = 20.0;
			std::cout << "[Stoner] SURPLUS: " << res.name 
			          << " (extra: " << -missing << ") - Priority: " << priority << "\n";
		}
		
		if (priority > 0.0) {
			bb.Bids.push_back(Bid(
				CommandEntry::Create(CommandType::Take, res.name, bb.CurrentTick),
				priority
			));
		}
	}
}

AgentStoner::~AgentStoner() {};
