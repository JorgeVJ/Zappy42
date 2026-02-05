#include "pch.h"
#include "Direction.h"

std::string DirectionToString(Direction dir)
{
    switch (dir)
    {
        case Direction::North:      return "North";
        case Direction::NorthEast:  return "NorthEast";
        case Direction::East:       return "East";
        case Direction::SouthEast:  return "SouthEast";
        case Direction::South:      return "South";
        case Direction::SouthWest:  return "SouthWest";
        case Direction::West:       return "West";
        case Direction::NorthWest:  return "NorthWest";
        default:                    return "Unknown";
    }
}

int DirectionToInt(Direction dir)
{
    return static_cast<int>(dir);
}