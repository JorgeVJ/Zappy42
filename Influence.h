#pragma once
#include <cstdint>
#include "Tile.h"

struct Influence {
    bool IsActive;
    uint8_t Steps;
    Tile* Origin;

    double Strength() const {
        return 100.0 * std::pow(0.8, Steps);
    }
};

class Influence
{
};

