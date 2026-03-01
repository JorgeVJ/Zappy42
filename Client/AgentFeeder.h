#pragma once
#include "IAgent.h"

class AgentFeeder :
    public IAgent
{
    // Heredado via IAgent
  public:
	  void GetBids(Blackboard& blackboard) override;
	  ~AgentFeeder() override;
};
