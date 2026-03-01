#pragma once
#include "IAgent.h"

class AgentBreeder :
    public IAgent
{
    // Heredado via IAgent
  public:
	  void GetBids(Blackboard& blackboard) override;
	  ~AgentBreeder() override;
};
