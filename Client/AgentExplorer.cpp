#include "AgentExplorer.h"
#include "Bid.h"
#include "CommandEntry.h"
#include "CommandType.h"
#include "Direction.h"
#include "ClientLog.h"


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
/// Calcula el factor de cooldown para "voir" usando el ultimo tick en que se ejecuto correctamente.
/// </summary>
double GetSeeCooldownFactor(const Blackboard& bb)
{
	const int SEE_COOLDOWN_TICKS = 21;
	const int age = bb.LastSeeTick < 0 ? 100000 : (bb.CurrentTick - bb.LastSeeTick);

	if (age <= 0)
		return 0.05;
	if (age < SEE_COOLDOWN_TICKS / 3)
		return 0.20;
	if (age < SEE_COOLDOWN_TICKS)
		return 0.60;
	return 1.0;
}

/// <summary>
/// Calcula la prioridad del comando "voir" basandose en el estado del mapa,
/// la frescura de la informacion y un cooldown para evitar bucles.
/// </summary>
double GetVoirScore(Blackboard& bb) {
	const double HIGH_PRIORITY = 100.0;  // Tile actual sin informacion
	const double MIN_SCORE = 50.0;       // Minimo para exploracion normal
	const double MAX_SCORE = 70.0;       // Maximo para exploracion normal
	const int DECAY_TICKS = 42;          // Ticks para considerar informacion vieja

	Tile* origin = bb.GetPlayerTile();
	if (!origin)
		return 0.0;

	const double seeCooldownFactor = GetSeeCooldownFactor(bb);

	// PRIORIDAD ALTA: Si el tile actual nunca se ha visto
	auto* originData = bb.explorationService.Registry.Get(origin);
	if (!originData || originData->LastSeenTick < 0) {
		LOG_EXPLORER("CRITICAL: Current tile unexplored! Priority: " << HIGH_PRIORITY);
		return (HIGH_PRIORITY * (1.0 - bb.GetHungerNeed())) * seeCooldownFactor;
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

	if (visibleTiles > 0) {
		voirScore /= visibleTiles;
	}

	double explorationPercentage = GetMapExplorationPercentage(bb);
	double explorationBonus = 1.0 - explorationPercentage;

	double finalScore = MIN_SCORE + (voirScore * explorationBonus * (MAX_SCORE - MIN_SCORE) / 100.0);
	if (finalScore < MIN_SCORE) finalScore = MIN_SCORE;
	if (finalScore > MAX_SCORE) finalScore = MAX_SCORE;

	finalScore *= (1.0 - bb.GetHungerNeed());
	finalScore *= seeCooldownFactor;

	LOG_EXPLORER("Voir Score: " << finalScore
			  << " | Exploration: " << (explorationPercentage * 100) << "% "
			  << "| Visible tiles staleness: " << voirScore
			  << " | Cooldown factor: " << seeCooldownFactor);

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
	const int FOOD_PRIORITY_THRESHOLD = 90;
	const double FOOD_REQUEST_WEIGHT = 8.0;
	const double RESOURCE_REQUEST_WEIGHT = 30.0;

	for (const auto& request : bb.ResourceRequests) {
		int age = bb.CurrentTick - request.tickRequested;
		if (age > MAX_AGE_TICKS)
			continue;

		if (request.resource == Resource::Food && request.priority < FOOD_PRIORITY_THRESHOLD)
			continue;

		// Factor de edad: requests recientes valen mas
		double ageMultiplier = 1.0 - (static_cast<double>(age) / MAX_AGE_TICKS);

		// Revisar si este tile tiene influencias del recurso solicitado
		auto* influence = bb.influenceService.Registry.Get(tile);
		if (influence) {
			for (const auto* inf : influence->Signals) {
				if (inf->resource == request.resource) {
					// Bonus escalado por prioridad y edad
					const double requestWeight = (request.resource == Resource::Food) ? FOOD_REQUEST_WEIGHT : RESOURCE_REQUEST_WEIGHT;
					double requestBonus = (request.priority / 100.0) * ageMultiplier * requestWeight;
					bonus += requestBonus;

					LOG_EXPLORER("Found influence for requested resource: "
						  << Inventory::ResourceToString(request.resource)
						  << " (priority: " << request.priority
						  << ", bonus: +" << requestBonus << ")");
				}
			}
		}
	}

	return bonus;
}

namespace
{
	struct MoveCandidate
	{
		Direction direction = Direction::North;
		double score = 0.0;
		bool valid = false;
	};

	CommandType GetTurnCommand(Direction from, Direction to)
	{
		return (GetTurnDirection(from, to) == TurnDirection::Right)
			? CommandType::Right
			: CommandType::Left;
	}

	MoveCandidate GetBestMoveCandidate(Blackboard& bb, Tile* currentTile)
	{
		MoveCandidate best;
		Direction directions[] = {
			Direction::North, Direction::South,
			Direction::East, Direction::West
		};

		for (Direction dir : directions)
		{
			Tile* neighbor = currentTile->GetNeighbor(dir);
			if (!neighbor)
				continue;

			double explorationValue = bb.explorationService.GetExplorationValue(neighbor, bb.CurrentTick, 100);
			double resourceBonus = GetResourceRequestBonus(bb, neighbor);
			double moveScore = 50.0 + (explorationValue / 10.0) + resourceBonus;
			if (moveScore > 90.0)
				moveScore = 90.0;

			double hungerPenalty = bb.GetHungerNeed();
			for (const auto& req : bb.ResourceRequests)
			{
				if (req.resource == Resource::Food && req.priority > 80)
				{
					hungerPenalty *= 0.3;
					break;
				}
			}

			moveScore *= (1.0 - hungerPenalty);

			if (!best.valid || moveScore > best.score)
			{
				best.direction = dir;
				best.score = moveScore;
				best.valid = true;
			}
		}

		return best;
	}
}

void AgentExplorer::GetBids(Blackboard& bb)
{
	bb.CleanupOldRequests(500);

	bb.Bids.push_back(Bid(
		CommandEntry::Create(CommandType::See, bb.CurrentTick),
		GetVoirScore(bb)
	));

	Tile* currentTile = bb.GetPlayerTile();
	if (!currentTile)
		return;

	if (!bb.ExplorerHasMovementPlan)
	{
		MoveCandidate bestMove = GetBestMoveCandidate(bb, currentTile);
		if (!bestMove.valid || bestMove.score <= 40.0)
			return;

		bb.ExplorerTargetDirection = bestMove.direction;
		bb.ExplorerHasMovementPlan = true;
	}

	Tile* targetTile = currentTile->GetNeighbor(bb.ExplorerTargetDirection);
	if (!targetTile)
	{
		bb.ExplorerHasMovementPlan = false;
		return;
	}

	const double explorationValue = bb.explorationService.GetExplorationValue(targetTile, bb.CurrentTick, 100);
	const double resourceBonus = GetResourceRequestBonus(bb, targetTile);
	double moveScore = 50.0 + (explorationValue / 10.0) + resourceBonus;
	if (moveScore > 90.0)
		moveScore = 90.0;

	double hungerPenalty = bb.GetHungerNeed();
	for (const auto& req : bb.ResourceRequests)
	{
		if (req.resource == Resource::Food && req.priority > 80)
		{
			hungerPenalty *= 0.3;
			break;
		}
	}

	moveScore *= (1.0 - hungerPenalty);
	if (moveScore <= 40.0)
		return;

	if (bb.Me.Orientation == bb.ExplorerTargetDirection)
	{
		LOG_EXPLORER("Advance bid towards " << DirectionToString(bb.ExplorerTargetDirection)
			<< " - Score: " << moveScore);

		bb.Bids.push_back(Bid(
			CommandEntry::Create(CommandType::Advance, bb.CurrentTick),
			moveScore + 5.0
		));
		return;
	}

	CommandType turnCommand = GetTurnCommand(bb.Me.Orientation, bb.ExplorerTargetDirection);
	LOG_EXPLORER("Turn bid towards " << DirectionToString(bb.ExplorerTargetDirection)
		<< " - Score: " << moveScore
		<< " | Turn: " << CommandTypeToString(turnCommand));

	bb.Bids.push_back(Bid(
		CommandEntry::Create(turnCommand, bb.CurrentTick),
		moveScore
	));
}

AgentExplorer::~AgentExplorer() {};
