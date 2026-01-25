#pragma once
#include "Blackboard.h"

class IAgent
{
	public:
		virtual void GetBids(Blackboard& blackboard) = 0;
};

