#pragma once
#include <string>
class Bid
{	
	public:
		std::string Command;
		double Value;
		Bid(std::string command, double value = 0);
};

