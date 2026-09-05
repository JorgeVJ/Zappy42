#include "AgentStoner.h"
#include "Bid.h"
#include "CommandEntry.h"
#include "Inventory.h"
#include "ClientLog.h"
#include "UtilityHelper.h"
#include <algorithm>
#include <vector>

namespace
{
	struct ResourceInfo
	{
		std::string name;
		Resource resource;
	};

	const ResourceInfo kResources[] = {
		{"linemate", Resource::Linemate},
		{"deraumere", Resource::Deraumere},
		{"sibur", Resource::Sibur},
		{"mendiane", Resource::Mendiane},
		{"phiras", Resource::Phiras},
		{"thystame", Resource::Thystame}
	};

	const IncantationRecipe* GetRecipeForLevel(int level)
	{
		auto it = Inventory::IncantationRecipes.find(level);
		if (it == Inventory::IncantationRecipes.end())
			return nullptr;
		return &it->second;
	}

	double NormalizeNeed(int current, int required)
	{
		if (required <= 0)
			return 0.0;

		const int missing = (required - current) > 0 ? (required - current) : 0;
		return UtilityHelper::Clamp01(static_cast<double>(missing) / static_cast<double>(required));
	}

	double BuildResourceUtility(const Blackboard& bb, Resource resource, int currentRequired, int nextRequired, int onTile)
	{
		const double hungerNeed = bb.GetHungerNeed();
		const double lifePressure = 1.0 - bb.GetLifePercentage();
		const double currentNeed = NormalizeNeed(bb.Me.inventory.Get(resource), currentRequired);
		const double nextNeed = NormalizeNeed(bb.Me.inventory.Get(resource), nextRequired);
		const double tileAvailability = UtilityHelper::Clamp01(static_cast<double>(onTile) / 3.0);

		const double currentUtility = UtilityHelper::EvaluatePerceptron(
			{ hungerNeed, lifePressure, currentNeed, tileAvailability },
			{ 2.2, 1.0, 2.8, 1.2 },
			-1.9,
			UtilityActivation::Sigmoid);

		const double nextUtility = UtilityHelper::EvaluatePerceptron(
			{ hungerNeed, lifePressure, nextNeed, tileAvailability },
			{ 1.7, 0.8, 2.1, 1.0 },
			-2.1,
			UtilityActivation::Sigmoid);

		return (currentUtility * 0.75) + (nextUtility * 0.25);
	}
}

void AgentStoner::GetBids(Blackboard& bb)
{
	Tile* playerTile = bb.GetPlayerTile();
	if (!playerTile)
		return;

	const IncantationRecipe* currentRecipe = GetRecipeForLevel(bb.Me.Level);
	if (!currentRecipe)
		return;

	const IncantationRecipe* nextRecipe = GetRecipeForLevel(bb.Me.Level + 1);

	LOG_STONER("Analyzing resources for level " << bb.Me.Level << " incantation");

	for (const auto& res : kResources)
	{
		const int onTile = playerTile->inventory.Get(res.resource);
		const int currentNeeded = currentRecipe->RequiredResources.Get(res.resource);
		const int nextNeeded = nextRecipe ? nextRecipe->RequiredResources.Get(res.resource) : 0;

		if (onTile <= 0)
		{
			const int currentMissing = (currentNeeded - bb.Me.inventory.Get(res.resource)) > 0 ? (currentNeeded - bb.Me.inventory.Get(res.resource)) : 0;
			const int nextMissing = (nextNeeded - bb.Me.inventory.Get(res.resource)) > 0 ? (nextNeeded - bb.Me.inventory.Get(res.resource)) : 0;
			if (currentMissing > 0 || nextMissing > 0)
			{
				const double searchUtility = BuildResourceUtility(bb, res.resource, currentNeeded, nextNeeded, 0);
				const double searchPriority = UtilityHelper::LinearClamp(searchUtility * 100.0, 0.0, 100.0);
				bb.RequestResource(res.resource, static_cast<int>(searchPriority));

				LOG_STONER("Requesting search for " << res.name
					<< " | current missing: " << currentMissing
					<< " | next missing: " << nextMissing
					<< " | priority: " << searchPriority);
			}

			continue;
		}

		const double utility = BuildResourceUtility(bb, res.resource, currentNeeded, nextNeeded, onTile);
		const double priority = UtilityHelper::LinearClamp(utility * 100.0, 0.0, 100.0);

		if (priority <= 0.0)
			continue;

		const int currentMissing = (currentNeeded - bb.Me.inventory.Get(res.resource)) > 0 ? (currentNeeded - bb.Me.inventory.Get(res.resource)) : 0;
		const int nextMissing = (nextNeeded - bb.Me.inventory.Get(res.resource)) > 0 ? (nextNeeded - bb.Me.inventory.Get(res.resource)) : 0;

		LOG_STONER("TAKE " << res.name
			<< " | tile: " << onTile
			<< " | current missing: " << currentMissing
			<< " | next missing: " << nextMissing
			<< " | priority: " << priority);

		bb.Bids.push_back(Bid(
			CommandEntry::Create(CommandType::Take, res.name, bb.CurrentTick),
			priority
		));
	}
}

AgentStoner::~AgentStoner() {};
