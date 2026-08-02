#include "AgentFeeder.h"
#include "Bid.h"
#include "CommandEntry.h"
#include "UtilityHelper.h"
#include <iostream>
#include <vector>

namespace
{
	// Build the input vector and weights for the "Take Food" utility function
	double BuildTakeFoodUtility(const Blackboard& bb, int foodOnTile)
	{
		const double hungerNeed = bb.GetHungerNeed();
		const double lifePressure = 1.0 - bb.GetLifePercentage();
		const double foodReserveNeed = bb.GetFoodReserveNeed();
		const double foodAvailability = UtilityHelper::Clamp01(static_cast<double>(foodOnTile) / 3.0);

		const std::vector<double> inputs = {
			hungerNeed,
			lifePressure,
			foodReserveNeed,
			foodAvailability
		};

		const std::vector<double> weights = {
			2.4,
			1.3,
			1.1,
			1.6
		};

		return UtilityHelper::EvaluatePerceptron(inputs, weights, -1.9, UtilityActivation::Sigmoid);
	}
	
	// Build the input vector and weights for the "Search Food" utility function
	double BuildSearchFoodUtility(const Blackboard& bb, int foodOnTile)
	{
		const double hungerNeed = bb.GetHungerNeed();
		const double lifePressure = 1.0 - bb.GetLifePercentage();
		const double foodReserveNeed = bb.GetFoodReserveNeed();
		const double scarcity = 1.0 - UtilityHelper::Clamp01(static_cast<double>(foodOnTile) / 3.0);

		const std::vector<double> inputs = {
			hungerNeed,
			lifePressure,
			foodReserveNeed,
			scarcity
		};

		const std::vector<double> weights = {
			2.0,
			1.1,
			0.8,
			1.2
		};

		return UtilityHelper::EvaluatePerceptron(inputs, weights, -1.6, UtilityActivation::Sigmoid);
	}
}

void AgentFeeder::GetBids(Blackboard& bb)
{
	bb.Me.UpdateFoodConsumption(bb.CurrentTick);

	const double hungerNeed = bb.GetHungerNeed();
	if (hungerNeed <= 0.30)
	{
		std::cout << "[Feeder] Hunger need is low (" << (hungerNeed * 100) << "%). No action needed.\n";
		return;
	}

	Tile* playerTile = bb.GetPlayerTile();
	if (!playerTile)
	{
		std::cout << "[Feeder] Empty player tile.\n";
		return;
	}

	const int foodOnTile = playerTile->inventory.Get(Resource::Food);
	const int currentFood = bb.Me.inventory.Get(Resource::Food);
	const int remainingTicks = bb.GetRemainingLifeTicks();

	std::cout << "[Feeder] Food=" << currentFood
		<< " | Remaining ticks=" << remainingTicks
		<< " | Tile food=" << foodOnTile
		<< " | Hunger need=" << hungerNeed << "\n";

	if (foodOnTile > 0)
	{
		const double takeUtility = BuildTakeFoodUtility(bb, foodOnTile);
		const double takePriority = UtilityHelper::LinearClamp(takeUtility * 100.0, 0.0, 100.0);

		std::cout << "[Feeder] TAKE utility=" << takeUtility
			<< " -> priority=" << takePriority << "\n";

		bb.Bids.push_back(Bid(
			CommandEntry::Create(CommandType::Take, "nourriture", bb.CurrentTick),
			takePriority
		));
		return;
	}

	const double searchUtility = BuildSearchFoodUtility(bb, foodOnTile);
	const double searchPriority = UtilityHelper::LinearClamp(searchUtility * 100.0, 0.0, 100.0);

	std::cout << "[Feeder] No food on tile. SEARCH utility=" << searchUtility
		<< " -> priority=" << searchPriority << "\n";

	bb.RequestResource(Resource::Food, static_cast<int>(searchPriority));
	bb.Bids.push_back(Bid(
		CommandEntry::Create(CommandType::See, bb.CurrentTick),
		searchPriority
	));
}

AgentFeeder::~AgentFeeder()
{
}
