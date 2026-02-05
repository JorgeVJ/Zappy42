#include "pch.h"
#include "CommandHistory.h"
#include "CommandType.h"
#include "CommandEntry.h"

CommandHistory::CommandHistory() {}

int GetCommandDelay(CommandType cmd)
{
    switch (cmd)
    {
    case CommandType::Advance:      return 7;
    case CommandType::Right:        return 7;
    case CommandType::Left:         return 7;
    case CommandType::See:          return 7;
    case CommandType::Inventory:    return 1;
    case CommandType::Take:         return 7;
    case CommandType::Put:          return 7;
    case CommandType::Expulse:      return 7;
    case CommandType::Broadcast:    return 7;
    case CommandType::Incantation:  return 300;
    case CommandType::Fork:         return 42;
    case CommandType::ConnectNbr:   return 0;
    case CommandType::Death:        return 0;
	default: return 0;
    }
    return 0;
}

void CommandHistory::AddCommand(CommandType type, long currentTick, std::string parameter)
{
    long start = currentTick;

    if (!commands.empty())
        start = commands.back().endTick;

    long delay = GetCommandDelay(type);
    long end = start + delay;

    commands.push_back({ type, parameter, start, end });
}

void CommandHistory::Update(long currentTick)
{
    while (!commands.empty() && commands.front().endTick <= currentTick)
        commands.pop_front();
}

bool CommandHistory::IsBusy(long currentTick) const
{
    if (commands.empty())
        return false;
    return commands.back().endTick > currentTick;
}

long CommandHistory::TimeUntilFree(long currentTick) const
{
    if (commands.empty())
        return 0;
    return commands.back().endTick - currentTick;
}

long CommandHistory::GetTotalPendingTime(long currentTick) const
{
    long total = 0;
    for (const auto& cmd : commands)
    {
        if (cmd.endTick > currentTick)
            total += cmd.endTick - std::max(cmd.startTick, currentTick);
    }
    return total;
}

const std::deque<CommandEntry>& CommandHistory::GetPendingCommands() const
{
    return commands;
}
