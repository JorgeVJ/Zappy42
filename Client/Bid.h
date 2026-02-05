#pragma once
#include <string>
#include "CommandType.h"

class Bid
{	
	public:
		std::string Command;
		CommandType Type;
		double Value;
		Bid(std::string command, double value = 0);
};

