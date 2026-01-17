#pragma once
#include "IAgent.h"

class AgentHungry :
    public IAgent
{
    // Heredado vía IAgent
    void GetBids(Blackboard& blackboard) override;
};

