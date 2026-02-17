#include "pch.h"
#include "CommandType.h"
#include <algorithm>
#include <cctype>
#include <map>

std::string GetCommandKeyword(const std::string& command)
{
    if (command.empty())
        return "";

    // Encontrar el primer espacio para extraer la palabra clave
    size_t spacePos = command.find(' ');
    std::string keyword = (spacePos != std::string::npos)
        ? command.substr(0, spacePos)
        : command;

    // Trim whitespace
    keyword.erase(0, keyword.find_first_not_of(" \t\n\r"));
    keyword.erase(keyword.find_last_not_of(" \t\n\r") + 1);

    // Convertir a minúsculas para comparación case-insensitive
    std::transform(keyword.begin(), keyword.end(), keyword.begin(),
        [](unsigned char c) { return std::tolower(c); });

    return keyword;
}

CommandType ParseCommandType(const std::string& command)
{
    std::string keyword = GetCommandKeyword(command);

    if (keyword.empty())
        return CommandType::Empty;

    // Mapear comandos franceses a CommandType
    if (keyword == "avance")
        return CommandType::Advance;
    else if (keyword == "droite")
        return CommandType::Right;
    else if (keyword == "gauche")
        return CommandType::Left;
    else if (keyword == "voir")
        return CommandType::See;
    else if (keyword == "inventaire")
        return CommandType::Inventory;
    else if (keyword == "prend")
        return CommandType::Take;
    else if (keyword == "pose")
        return CommandType::Put;
    else if (keyword == "expulse")
        return CommandType::Expulse;
    else if (keyword == "broadcast")
        return CommandType::Broadcast;
    else if (keyword == "incantation")
        return CommandType::Incantation;
    else if (keyword == "fork")
        return CommandType::Fork;
    else if (keyword == "egg_hatched")
        return CommandType::EggHatched;
    else if (keyword == "connect_nbr")
        return CommandType::ConnectNbr;
    else if (keyword == "mort")
        return CommandType::Death;

    return CommandType::Invalid;
}

std::string CommandTypeToString(CommandType type)
{
    switch (type)
    {
    case CommandType::Advance:      return "avance";
    case CommandType::Right:        return "droite";
    case CommandType::Left:         return "gauche";
    case CommandType::See:          return "voir";
    case CommandType::Inventory:    return "inventaire";
    case CommandType::Take:         return "prend";
    case CommandType::Put:          return "pose";
    case CommandType::Expulse:      return "expulse";
    case CommandType::Broadcast:    return "broadcast";
    case CommandType::Incantation:  return "incantation";
    case CommandType::Fork:         return "fork";
    case CommandType::EggHatched:   return "egg_hatched";
    case CommandType::ConnectNbr:   return "connect_nbr";
    case CommandType::Death:        return "mort";
    case CommandType::Empty:        return "[empty]";
    case CommandType::Invalid:      return "[invalid]";
    default:                        return "[unknown]";
    }
}

int GetCommandDuration(CommandType type)
{
    // Duración en ticks según el subject de Zappy
    static const std::map<CommandType, int> durations = {
        {CommandType::Advance,      7},
        {CommandType::Right,        7},
        {CommandType::Left,         7},
        {CommandType::See,          7},
        {CommandType::Inventory,    1},
        {CommandType::Take,         7},
        {CommandType::Put,          7},
        {CommandType::Expulse,      7},
        {CommandType::Broadcast,    7},
        {CommandType::Incantation,  300},
        {CommandType::Fork,         42}, // Time to lay an egg
        {CommandType::EggHatched,   600}, // Time for the egg to hatch 
        {CommandType::ConnectNbr,   0},  // No consume tiempo
        {CommandType::Death,        0},  // No consume tiempo
        {CommandType::Empty,        0},
        {CommandType::Invalid,      0}
    };

    auto it = durations.find(type);
    if (it != durations.end())
        return it->second;
    
    return 0; // Por defecto
}