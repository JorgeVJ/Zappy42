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

enum class TurnDirection {
    Right,
    Left
};

std::string DirectionToString(Direction dir);
int DirectionToInt(Direction dir);

/// <summary>
/// Convierte un numero de broadcast (1-8) a una direccion absoluta
/// considerando la orientacion actual del jugador
/// </summary>
/// <param name="soundNumber">Numero del cuadrado (1-8, o 0 si esta en el mismo tile)</param>
/// <param name="playerOrientation">Orientacion actual del jugador</param>
/// <returns>Direccion absoluta desde donde viene el sonido</returns>
Direction BroadcastNumberToDirection(int soundNumber, Direction playerOrientation);

/// <summary>
/// Obtiene la direccion opuesta a la dada
/// </summary>
Direction GetOppositeDirection(Direction dir);