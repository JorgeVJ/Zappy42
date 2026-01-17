#pragma once
#include "IAgent.h"

class AgentStoner : public IAgent
{
	// Heredado vía IAgent
	void GetBids(Blackboard& blackboard) override;
};

