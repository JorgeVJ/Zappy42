#pragma once
#include "IAgent.h"

class AgentFeeder :
    public IAgent
{
    // Heredado vía IAgent
  public:
	  void GetBids(Blackboard& blackboard) override;
	  ~AgentFeeder() override;
};
