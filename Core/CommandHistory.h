// Core/CommandHistory.hpp
#pragma once
#include <deque>
#include "CommandEntry.h"

class CommandHistory
{
public:
    CommandHistory();

    void AddCommand(CommandType type, long currentTick, std::string parameter);
    void Update(long currentTick);
    bool IsBusy(long currentTick) const;
    long TimeUntilFree(long currentTick) const;
    long GetTotalPendingTime(long currentTick) const;
    const std::deque<CommandEntry>& GetPendingCommands() const;

private:
    std::deque<CommandEntry> commands;
};
