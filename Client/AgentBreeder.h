#pragma once
#include "IAgent.h"

class AgentBreeder :
    public IAgent
{
    // Heredado vía IAgent
    void GetBids(Blackboard& blackboard) override;
};

