#pragma once
#include "CommandType.h"

struct CommandEntry
{
    CommandType type;
    std::string commandParameter;
    long startTick;
    long endTick;
};
