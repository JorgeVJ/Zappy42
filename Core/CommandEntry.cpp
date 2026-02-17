#include "pch.h"
#include "CommandEntry.h"
#include "CommandType.h"

CommandEntry CommandEntry::Create(CommandType cmdType, const std::string& param, long currentTick)
{
    return CommandEntry{
        cmdType,
        param,
        currentTick,
        currentTick + GetCommandDuration(cmdType)
    };
}

CommandEntry CommandEntry::Create(CommandType cmdType, long currentTick)
{
    return Create(cmdType, "", currentTick);
}