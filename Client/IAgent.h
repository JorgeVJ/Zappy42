#pragma once
#include "Blackboard.h"

class IAgent
{
  public:
	  virtual ~IAgent();
	  virtual void GetBids(Blackboard& blackboard) = 0;
};
