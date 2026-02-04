#pragma once
#include <string>

enum class CommandType
{
    Advance,
    Right,
    Left,
    See,
    Inventory,
    Take,
    Put,
    Expulse,
    Broadcast,
    Incantation,
    Fork,
    ConnectNbr,
    Death,
    Empty,
    Invalid
};

// Parsea un comando y devuelve su tipo
CommandType ParseCommandType(const std::string& command);

// Convierte un CommandType a string (útil para debugging/logging)
std::string CommandTypeToString(CommandType type);

// Obtiene la palabra clave del comando (primera palabra)
std::string GetCommandKeyword(const std::string& command);
