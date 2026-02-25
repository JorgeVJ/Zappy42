#pragma once
#include "IAgent.h"

class AgentHungry :
    public IAgent
{
    // Heredado vía IAgent
  public:
	  void GetBids(Blackboard& blackboard) override;
	  ~AgentHungry() override;
};
