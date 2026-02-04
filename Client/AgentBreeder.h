#pragma once
#include "IAgent.h"

class AgentBreeder :
    public IAgent
{
    // Heredado vía IAgent
  public:
	  void GetBids(Blackboard& blackboard) override;
	  ~AgentBreeder() override;
};
