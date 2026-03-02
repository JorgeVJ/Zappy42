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
/// Convierte un número de broadcast (1-8) a una dirección absoluta
/// considerando la orientación actual del jugador
/// </summary>
/// <param name="soundNumber">Número del cuadrado (1-8, o 0 si está en el mismo tile)</param>
/// <param name="playerOrientation">Orientación actual del jugador</param>
/// <returns>Dirección absoluta desde donde viene el sonido</returns>
Direction BroadcastNumberToDirection(int soundNumber, Direction playerOrientation);

/// <summary>
/// Obtiene la dirección opuesta a la dada
/// </summary>
Direction GetOppositeDirection(Direction dir);