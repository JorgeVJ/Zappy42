#pragma once
#include <string>
#include "Point.h"
#include "Direction.h"
#include "Inventory.h"

struct Player
{
    int ID = -1;
    int Level = 1;
    Point Position = {0, 0};
    Direction Orientation = Direction::North;
    Inventory inventory;
    std::string TeamName = "";

    Player() = default;

    Player(int id, const std::string& teamName, Point position = {0, 0}, Direction orientation = Direction::North)
        : ID(id), TeamName(teamName), Position(position), Orientation(orientation)
    {
    }
};
