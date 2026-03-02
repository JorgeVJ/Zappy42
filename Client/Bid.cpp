#include "Bid.h"
#include "CommandType.h"
#include "CommandEntry.h"


Bid::Bid(CommandEntry command, double value) : Command(command), Value(value)
{
}
