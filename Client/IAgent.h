#pragma once
#include "Blackboard.h"

class IAgent
{
  public:
	  virtual ~IAgent() = 0;
	  virtual void GetBids(Blackboard& blackboard) = 0;
};
