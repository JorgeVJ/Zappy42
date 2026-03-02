#include "AgentFeeder.h"
#include "Bid.h"
#include "CommandEntry.h"
#include <iostream>

void AgentFeeder::GetBids(Blackboard& bb)
{
	// actualizar inventario de comida segun los ticks transcurridos (cada 126 ticks se consume 1 comida). Guardar tiempo de ultima actualizacion (ticks) para saber desde cuando no se ha actualizado.
	bb.Me.UpdateFoodConsumption(bb.CurrentTick);
	double hungerNeed = bb.GetHungerNeed();


	//Necesito comida? Si no, no hago nada
	//if (hungerNeed < 0.15) {
	//	std::cout << "[Feeder] Hunger need is low (" << (bb.GetHungerNeed() * 100) << "%). No action needed.\n";
	//	return;
	//}


	Tile* playerTile = bb.GetPlayerTile();
	if (!playerTile)
		return;

	// Verificar si hay comida en el tile actual
	int foodOnTile = playerTile->inventory.Get(Resource::Food);
	int currentFood = bb.Me.inventory.Get(Resource::Food);
	int remainingTicks = bb.GetRemainingLifeTicks();
	
	// SOLICITAR BuSQUEDA DE COMIDA segun urgencia
	int searchPriority = 0;
	
	if (remainingTicks <= 100) {
		searchPriority = 150; // MUERTE INMINENTE
	}
	else if (remainingTicks < 200) {
		searchPriority = 95; // CRiTICO
	}
	else if (remainingTicks < 400) {
		searchPriority = 85; // URGENTE
	}
	else if (remainingTicks < 600) {
		searchPriority = 70; // ALTO
	}
	else if (remainingTicks < 800) {
		searchPriority = 50; // MEDIO
	}
	else if (remainingTicks < 1000) {
		searchPriority = 30; // BAJO
	}
	else {
		searchPriority = 15; // MUY BAJO
	}
	
	// Solicitar busqueda de comida si la prioridad es significativa
	if (searchPriority > 30) {
		bb.RequestResource(Resource::Food, searchPriority);
		std::cout << "[Feeder] Requesting FOOD search with priority: " << searchPriority << "\n";
	}
	
	// Si NO hay comida en el tile actual, solo solicitamos busqueda
	if (foodOnTile <= 0) {
		std::cout << "[Feeder] No food on current tile. Waiting for exploration...\n";
		return;
	}
	
	// Si HAY comida aqui, hacer bid para recogerla
	double priority = 0.0;
	std::string urgencyLevel;
	
	if (remainingTicks <= 0) {
		priority = 250.0;
		urgencyLevel = "DEATH";
		std::cout << "[Feeder] DEATH IMMINENT! Priority: " << priority << "\n";
	}
	else if (remainingTicks < 200) {
		priority = 200.0;
		urgencyLevel = "CRITICAL";
		std::cout << "[Feeder] 🔴 CRITICAL: Only " << remainingTicks << " ticks left ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	else if (remainingTicks < 400) {
		priority = 150.0;
		urgencyLevel = "URGENT";
		std::cout << "[Feeder] 🟠 URGENT: " << remainingTicks << " ticks remaining ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	else if (remainingTicks < 600) {
		priority = 100.0;
		urgencyLevel = "HIGH";
		std::cout << "[Feeder] 🟡 HIGH: " << remainingTicks << " ticks remaining ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	else if (remainingTicks < 800) {
		priority = 70.0;
		urgencyLevel = "MEDIUM";
		std::cout << "[Feeder] 🔵 MEDIUM: " << remainingTicks << " ticks remaining ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	else if (remainingTicks < 1000) {
		priority = 40.0;
		urgencyLevel = "LOW";
		std::cout << "[Feeder] 🟢 LOW: " << remainingTicks << " ticks remaining ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	else {
		priority = 10.0;
		urgencyLevel = "VERY LOW";
		std::cout << "[Feeder] ✅ VERY LOW: " << remainingTicks << " ticks remaining ("
		          << currentFood << " food). Priority: " << priority << "\n";
	}
	
	std::cout << "[Feeder] Life: " << (hungerNeed * 100) << "% hunger need | "
	          << "Can survive: ~" << (remainingTicks / 7) << " commands\n";
	
	bb.Bids.push_back(Bid(
		CommandEntry::Create(CommandType::Take, "nourriture", bb.CurrentTick),
		priority
	));
}

AgentFeeder::~AgentFeeder()
{
}