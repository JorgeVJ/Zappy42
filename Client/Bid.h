#pragma once
#include <string>
#include "CommandType.h"
#include "CommandEntry.h"

class Bid
{	
	public:
		CommandEntry Command;
		double Value;
		Bid(CommandEntry command, double value = 0);
};



