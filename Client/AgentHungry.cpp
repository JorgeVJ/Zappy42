#include "AgentHungry.h"
#include "Bid.h"
#include "CommandEntry.h"
#include <iostream>

double CalculateHunger(Blackboard& bb)
{
	double bias = 400;
	return bias * bb.GetHungerNeed();
}

void AgentHungry::GetBids(Blackboard& bb)
{
	Tile* playerTile = bb.GetPlayerTile();
	if (!playerTile)
		return;

	// Verificar si hay comida en el tile actual
	int foodOnTile = playerTile->inventory.Get(Resource::Food);
	if (foodOnTile <= 0)
		return; // No hay comida que recoger

	// Obtener estado de vida
	int remainingTicks = bb.GetRemainingLifeTicks();
	int currentFood = bb.Me.inventory.Get(Resource::Food);
	double lifePercentage = bb.GetLifePercentage();
	
	const int TICKS_PER_FOOD = 126;
	
	// Calcular prioridad basada en urgencia
	double priority = 0.0;
	std::string urgencyLevel;
	
	if (remainingTicks <= 0)
	{
		// MUERTE INMINENTE
		priority = 250.0;
		urgencyLevel = "DEATH";
		std::cout << "[Hungry] ☠️ DEATH IMMINENT! Priority: " << priority << "\n";
	}
	else if (remainingTicks < 200)
	{
		// CRÍTICO: Menos de 2 unidades de comida
		priority = 200.0;
		urgencyLevel = "CRITICAL";
		std::cout << "[Hungry] 🔴 CRITICAL: Only " << remainingTicks << " ticks left ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	else if (remainingTicks < 400)
	{
		// URGENTE: Menos de 3-4 unidades
		priority = 150.0;
		urgencyLevel = "URGENT";
		std::cout << "[Hungry] 🟠 URGENT: " << remainingTicks << " ticks remaining ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	else if (remainingTicks < 600)
	{
		// ALTO: Menos de 5 unidades
		priority = 100.0;
		urgencyLevel = "HIGH";
		std::cout << "[Hungry] 🟡 HIGH: " << remainingTicks << " ticks remaining ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	else if (remainingTicks < 800)
	{
		// MEDIO: Entre 6-7 unidades
		priority = 60.0;
		urgencyLevel = "MEDIUM";
		std::cout << "[Hungry] 🔵 MEDIUM: " << remainingTicks << " ticks remaining ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	else if (remainingTicks < 1000)
	{
		// BAJO: Entre 8-9 unidades
		priority = 30.0;
		urgencyLevel = "LOW";
		std::cout << "[Hungry] 🟢 LOW: " << remainingTicks << " ticks remaining ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	else
	{
		// MUY BAJO: 10+ unidades (bien de comida)
		priority = 10.0;
		urgencyLevel = "VERY LOW";
		std::cout << "[Hungry] ✅ VERY LOW: " << remainingTicks << " ticks remaining ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	
	// Información adicional de debugging
	std::cout << "[Hungry] Life: " << (lifePercentage * 100) << "% | "
			  << "Can survive: ~" << (remainingTicks / 7) << " commands\n";

	bb.Bids.push_back(Bid(
		CommandEntry::Create(CommandType::Take, "nourriture", bb.CurrentTick),
		priority
	));
}

AgentHungry::~AgentHungry()
{
}