#include "AgentFeeder.h"
#include "Bid.h"
#include "CommandEntry.h"
#include <iostream>

void AgentFeeder::GetBids(Blackboard& bb)
{
	// actualizar inventario de comida segun los ticks transcurridos (cada 126 ticks se consume 1 comida). Guardar tiempo de ultima actualizacion (ticks) para saber desde cuando no se ha actualizado.
	bb.Me.UpdateFoodConsumption(bb.CurrentTick);
	double hungerNeed = bb.GetHungerNeed();


	//Necesito comida? Si no, tiene sentido continuar?
	if (hungerNeed <= 0.30) {
		std::cout << "[Feeder] Hunger need is low (" << (bb.GetHungerNeed() * 100) << "%). No action needed.\n";
		return;
	}


	Tile* playerTile = bb.GetPlayerTile();
	if (!playerTile)
	{
		std::cout << "[Debug] Empty Player tile." << std::endl;
		return;
	}

	// Verificar si hay comida en el tile actual
	int foodOnTile = playerTile->inventory.Get(Resource::Food);
	int currentFood = bb.Me.inventory.Get(Resource::Food);
	int remainingTicks = bb.GetRemainingLifeTicks();
	int searchPriority = 0;
	double priority = 0.0;
	std::string urgencyLevel;

	if (remainingTicks <= 100) {
		priority = 250.0;
		urgencyLevel = "DEATH";
		searchPriority = 150; // MUERTE INMINENTE for resource request
	}
	else if (remainingTicks < 200) {
		priority = 200.0;
		urgencyLevel = "CRITICAL";
		searchPriority = 120; // CRiTICO
	}
	else if (remainingTicks < 400) {
		priority = 170.0;
		urgencyLevel = "URGENT";
		searchPriority = 85; // URGENTE
	}
	else if (remainingTicks < 600) {
		priority = 100.0;
		urgencyLevel = "HIGH";
		searchPriority = 70; // ALTO
	}
	else if (remainingTicks < 800) {
		priority = 70.0;
		urgencyLevel = "MEDIUM";
		searchPriority = 50; // MEDIO
	}
	else if (remainingTicks < 1000) {
		priority = 40.0;
		urgencyLevel = "LOW";
		searchPriority = 30; // BAJO
	}
	else {
		priority = 10.0;
		urgencyLevel = "VERY LOW";
		searchPriority = 15; // MUY BAJO
	}


	// Si HAY comida aqui, hacer bid para recogerla
	if (foodOnTile > 0){
		bb.Bids.push_back(Bid(
			CommandEntry::Create(CommandType::Take, "nourriture", bb.CurrentTick),
			priority
		));

		std::cout << "[Feeder] Life: " << (hungerNeed * 100) << "% hunger need " << priority << " |  ("
			<< currentFood << " food).\n";
	}
	// Si NO hay comida en el tile actual, solo solicitamos busqueda
	else {
		std::cout << "[Debug] No food on current tile. Create request and wait for exploration...\n";
		// Solicitar busqueda de comida si la prioridad es significativa
		if (searchPriority > 40) {
			bb.RequestResource(Resource::Food, searchPriority);
			std::cout << "[Feeder] Requesting FOOD search with priority: " << searchPriority << "\n";
		}
	}
	return;
}

AgentFeeder::~AgentFeeder()
{
}