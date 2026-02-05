#pragma once
#include <string>

enum class Direction {
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest
};

/// <summary>
/// Dirección de giro para el jugador
/// </summary>
enum class TurnDirection {
    Right,  // Giro a la derecha (sentido horario)
    Left    // Giro a la izquierda (sentido antihorario)
};

/// <summary>
/// Convierte una dirección a su representación en string
/// </summary>
std::string DirectionToString(Direction dir);

/// <summary>
/// Convierte una dirección a su índice numérico (0-7)
/// </summary>
int DirectionToInt(Direction dir);