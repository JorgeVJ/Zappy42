#pragma once
#include "IAgent.h"
#include "Inventory.h"
#include <map>

class AgentChaman : public IAgent
{
  public:
	void GetBids(Blackboard& blackboard) override;
	~AgentChaman() override;
};
