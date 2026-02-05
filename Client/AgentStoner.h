#pragma once
#include "IAgent.h"

class AgentStoner : public IAgent
{
	// Heredado vía IAgent
  public:
	void GetBids(Blackboard& blackboard) override;
	~AgentStoner() override;
};
