#pragma once
#include <string>
#include "Point.h"
#include "Direction.h"
#include "Inventory.h"

struct Player
{
    int ID;
    int Level = 1;
    Point Position;
    Direction Orientation;
    Inventory inventory;
    std::string TeamName;
};
