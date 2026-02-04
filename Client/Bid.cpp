#include "Bid.h"
#include "CommandType.h"


Bid::Bid(std::string command, double value) : Command(command), Value(value)
{
	Type = ParseCommandType(command);
}
