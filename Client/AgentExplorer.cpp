#include "AgentExplorer.h"
#include "Bid.h"
#include "CommandEntry.h"
#include <iostream>


/// <summary>
/// Calcula cuantos tiles del mapa han sido explorados
/// </summary>
double GetMapExplorationPercentage(Blackboard& bb) {
	int totalTiles = bb.map.Width * bb.map.Height;
	int exploredTiles = 0;

	for (int y = 0; y < bb.map.Height; ++y) {
		for (int x = 0; x < bb.map.Width; ++x) {
			Tile* tile = bb.map.GetTile(x, y);
			if (!tile)
				continue;
			
			// Verificar si el tile ha sido explorado (LastSeenTick != -1)
			auto* data = bb.explorationService.Registry.Get(tile);
			if (data && data->LastSeenTick >= 0) {
				exploredTiles++;
			}
		}
	}

	return static_cast<double>(exploredTiles) / totalTiles;
}
/// <summary>
/// Calcula la prioridad del comando "voir" basandose en:
/// - Si el tile actual nunca se ha visto: prioridad alta (100)
/// - Cuanto tiempo hace que se exploraron los tiles visibles
/// - El porcentaje de mapa explorado
/// - El nivel de hambre del jugador
/// </summary>
double GetVoirScore(Blackboard& bb) {
	const double HIGH_PRIORITY = 100.0;  // Tile actual sin informacion
	const double MIN_SCORE = 50.0;       // Minimo para exploracion normal
	const double MAX_SCORE = 70.0;       // Maximo para exploracion normal
	const int DECAY_TICKS = 42;          // Ticks para considerar informacion vieja

	Tile* origin = bb.GetPlayerTile();
	if (!origin)
		return 0.0;

	// PRIORIDAD ALTA: Si el tile actual nunca se ha visto
	//if (origin->inventory.Size() == 0)
	//	return bias * (1 - bb.GetHungerNeed());
	auto* originData = bb.explorationService.Registry.Get(origin);
	if (!originData || originData->LastSeenTick < 0) {
		std::cout << "[Explorer] CRITICAL: Current tile unexplored! Priority: " << HIGH_PRIORITY << "\n";
		return HIGH_PRIORITY * (1.0 - bb.GetHungerNeed());
	}

	// Calcular valor de exploracion de tiles visibles
	double voirScore = 0.0;
	std::vector<std::pair<int, int>> offsets = bb.GetVoirOffsets(bb.Me.Level, bb.Me.Orientation);
	int visibleTiles = 0;

	for (size_t i = 0; i < offsets.size(); ++i) {
		int dx = offsets[i].first;
		int dy = offsets[i].second;
		Tile* tile = bb.map.GetTile(origin->X + dx, origin->Y + dy);
		if (!tile)
			continue;

		visibleTiles++;
		voirScore += bb.explorationService.GetExplorationValue(tile, bb.CurrentTick, DECAY_TICKS);
	}

	// Normalizar el score por numero de tiles visibles
	if (visibleTiles > 0) {
		voirScore /= visibleTiles;
	}

	// Calcular bonus por porcentaje de mapa sin explorar
	double explorationPercentage = GetMapExplorationPercentage(bb);
	double explorationBonus = 1.0 - explorationPercentage; // Mas bonus si hay menos mapa explorado

	// Score final entre MIN_SCORE y MAX_SCORE
	double finalScore = MIN_SCORE + (voirScore * explorationBonus * (MAX_SCORE - MIN_SCORE) / 100.0);
	
	// Clamped entre MIN y MAX
	if (finalScore < MIN_SCORE) finalScore = MIN_SCORE;
	if (finalScore > MAX_SCORE) finalScore = MAX_SCORE;

	// Reducir por hambre
	finalScore *= (1.0 - bb.GetHungerNeed());

	std::cout << "[Explorer] Voir Score: " << finalScore 
	          << " | Exploration: " << (explorationPercentage * 100) << "% "
	          << "| Visible tiles staleness: " << voirScore << "\n";

	return finalScore;
}

/// <summary>
/// Calcula bonus de exploracion basandose en solicitudes de recursos
/// Prioriza especialmente la comida si hay solicitudes urgentes (>80)
/// </summary>
double GetResourceRequestBonus(Blackboard& bb, Tile* tile) {
	if (bb.ResourceRequests.empty())
		return 0.0;
	
	double bonus = 0.0;
	const int MAX_AGE_TICKS = 500;
	
	for (const auto& request : bb.ResourceRequests) {
		int age = bb.CurrentTick - request.tickRequested;
		if (age > MAX_AGE_TICKS)
			continue;
		
		// Factor de edad: requests recientes valen mas
		double ageMultiplier = 1.0 - (static_cast<double>(age) / MAX_AGE_TICKS);
		
		// Revisar si este tile tiene influencias del recurso solicitado
		auto* influence = bb.influenceService.Registry.Get(tile);
		if (influence) {
			for (const auto* inf : influence->Signals) {
				if (inf->resource == request.resource) {
					// Bonus escalado por prioridad y edad
					double requestBonus = (request.priority / 100.0) * ageMultiplier * 30.0;
					bonus += requestBonus;
					
					std::cout << "[Explorer] Found influence for requested resource: "
					          << Inventory::ResourceToString(request.resource)
					          << " (priority: " << request.priority 
					          << ", bonus: +" << requestBonus << ")\n";
				}
			}
		}
	}
	
	return bonus;
}

void AgentExplorer::GetBids(Blackboard& bb)
{
	// Limpiar requests antiguas
	bb.CleanupOldRequests(500);
	
	// Bid para "voir"
	double voirScore = GetVoirScore(bb);
	bb.Bids.push_back(Bid(
		CommandEntry::Create(CommandType::See, bb.CurrentTick), 
		voirScore
	));

	// Bids de movimiento hacia recursos solicitados
	Tile* currentTile = bb.GetPlayerTile();
	if (!currentTile)
		return;
	
	Direction directions[] = {
		Direction::North, Direction::South, 
		Direction::East, Direction::West
	};
	
	for (Direction dir : directions) {
		Tile* neighbor = currentTile->GetNeighbor(dir);
		if (!neighbor)
			continue;
		
		// Valor de exploracion base
		double explorationValue = bb.explorationService.GetExplorationValue(
			neighbor, bb.CurrentTick, 100
		);
		
		// Bonus por recursos solicitados (comida urgente da mas bonus)
		double resourceBonus = GetResourceRequestBonus(bb, neighbor);
		
		// Score: 50-60 base + exploracion + bonus de requests
		double moveScore = 50.0 + (explorationValue / 10.0) + resourceBonus;
		
		// Clamped (puede superar 70 si hay requests muy urgentes)
		if (moveScore > 90.0) moveScore = 90.0;
		
		// Reducir por hambre (pero no tanto si esta buscando comida)
		double hungerPenalty = bb.GetHungerNeed();
		bool searchingFood = false;
		for (const auto& req : bb.ResourceRequests) {
			if (req.resource == Resource::Food && req.priority > 80) {
				searchingFood = true;
				hungerPenalty *= 0.3; // Reducir penalizacion si busca comida urgente
				break;
			}
		}
		moveScore *= (1.0 - hungerPenalty);
		
		if (moveScore > 40.0) {
			std::cout << "[Explorer] Move bid towards " << DirectionToString(dir)
			          << " - Score: " << moveScore 
			          << " (exploration: " << explorationValue 
			          << ", resource bonus: " << resourceBonus << ")\n";
			
			// TODO: Generar comandos de giro si es necesario
			if (dir == bb.Me.Orientation) {
				bb.Bids.push_back(Bid(
					CommandEntry::Create(CommandType::Advance, bb.CurrentTick),
					moveScore
				));
			}
			// Check direction and send turn commands
			else {
				// Calcular giro necesario
				int turnSteps = (static_cast<int>(dir) - static_cast<int>(bb.Me.Orientation) + 4) % 4;
				CommandType turnCommand = (turnSteps == 1) ? CommandType::Right : CommandType::Left;

				bb.Bids.push_back(Bid(
					CommandEntry::Create(turnCommand, bb.CurrentTick), moveScore
				));
			}		
		}
	}
}

AgentExplorer::~AgentExplorer() {};
