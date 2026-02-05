#pragma once
#include "Blackboard.h"

class IAgent
{
  public:
      virtual ~IAgent() = default;
      virtual void GetBids(Blackboard& blackboard) = 0;
};
