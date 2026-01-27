#pragma once
#include "CommandType.h"

struct CommandEntry
{
    CommandType type;
    long startTick;
    long endTick;
};
