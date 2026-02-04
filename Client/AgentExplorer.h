#pragma once
#include "IAgent.h"

class AgentExplorer :
    public IAgent
{
  public:
	void GetBids(Blackboard& blackboard) override;
	~AgentExplorer() override;
};
