#pragma once
#include "IAgent.h"

class AgentStoner : public IAgent
{
	// Heredado via IAgent
  public:
	void GetBids(Blackboard& blackboard) override;
	~AgentStoner() override;
};
