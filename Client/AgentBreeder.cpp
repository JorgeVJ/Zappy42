#include "AgentBreeder.h"
#include "Bid.h"
#include <iostream>

void AgentBreeder::GetBids(Blackboard& bb)
{
	// double voirWeigth = 1;
	// double bias = 100;
	// double voirScore = bias + 5 * voirWeigth;

	// double score = voirScore * (1 - bb.GetHungerNeed());

	// Si ya tenemos el máximo de jugadores, no hacer nada
	if (bb.TeamNbr >= 6)
		return ;
	
	// Verificar slots disponibles. Enviar directamente desde aqui?
	CommandEntry connectNbr = { CommandType::ConnectNbr, "", bb.CurrentTick, 0 };
	bb.Bids.push_back(Bid(connectNbr, 10.0));

	// Fork si no hay suficientes slots disponibles
	// Verificar si necesitamos más jugadores para incantación
	auto it = Inventory::IncantationRecipes.find(bb.Me.Level);
	if (it != Inventory::IncantationRecipes.end())
	{
		const IncantationRecipe& recipe = it->second;
		int playersForIncantation = recipe.RequiredPlayers;
		int totalAvailablePlayers = bb.ConnectNbr + bb.TeamNbr; // Check if this is correct or keep just our count of teamNbr

		// Calcular prioridad basada en la necesidad
		double forkPriority = 0.0;

		if (totalAvailablePlayers < playersForIncantation)
		{
			// ALTA PRIORIDAD: No hay suficientes jugadores para incantación
			int missingPlayers = playersForIncantation - totalAvailablePlayers;
			forkPriority = 80.0 + (missingPlayers * 20.0); // Más prioritario cuanto más falten
		}
		else if (totalAvailablePlayers < 6)
		{
			// PRIORIDAD MEDIA: Tenemos suficientes para incantación, pero podemos crecer
			forkPriority = 40.0;
		}
		else
		{
			// PRIORIDAD BAJA: Ya tenemos muchos jugadores
			forkPriority = 15.0;
		}

		// Verificar que hay espacio para un nuevo jugador
		if (bb.TeamNbr + 1 <= 6)
		{
			bb.Bids.push_back(Bid(
				CommandEntry::Create(CommandType::Fork, bb.CurrentTick),
				forkPriority * (1 - bb.GetHungerNeed())
			));
		}
	}
}

AgentBreeder::~AgentBreeder() {};
