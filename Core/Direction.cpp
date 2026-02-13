#include "pch.h"
#include "Direction.h"
#include <iostream>

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

Direction BroadcastNumberToDirection(int soundNumber, Direction playerOrientation)
{
    // Si soundNumber es 0, está en el mismo tile (no hay dirección)
    if (soundNumber == 0)
    {
        std::cerr << "[Warning] Check Handle sound from same tile (number 0)\n";
        return playerOrientation; // Retorna la misma orientación
    }
    
    // Validar rango
    if (soundNumber < 1 || soundNumber > 8)
    {
        std::cerr << "[Error] Invalid sound number: " << soundNumber << "\n";
        return playerOrientation;
    }
    
    // Mapeo de números a direcciones relativas (antihorario desde enfrente)
    // 1 = enfrente, 2 = enfrente-izquierda, 3 = izquierda, 4 = atrás-izquierda
    // 5 = atrás, 6 = atrás-derecha, 7 = derecha, 8 = enfrente-derecha
    static const Direction relativeDirections[8] = {
        Direction::North,      // 1: enfrente
        Direction::NorthWest,  // 2: enfrente-izquierda (antihorario)
        Direction::West,       // 3: izquierda
        Direction::SouthWest,  // 4: atrás-izquierda
        Direction::South,      // 5: atrás
        Direction::SouthEast,  // 6: atrás-derecha
        Direction::East,       // 7: derecha
        Direction::NorthEast   // 8: enfrente-derecha
    };
    
    // Obtener dirección relativa
    Direction relativeDir = relativeDirections[soundNumber - 1];
    
    // Calcular rotación necesaria basada en la orientación del jugador
    // Solo consideramos las 4 direcciones cardinales para la orientación
    int rotation = 0;
    switch (playerOrientation)
    {
        case Direction::North:
            rotation = 0;
            break;
        case Direction::East:
            rotation = 2; // 90° a la derecha = 2 posiciones
            break;
        case Direction::South:
            rotation = 4; // 180° = 4 posiciones
            break;
        case Direction::West:
            rotation = 6; // 270° a la derecha = 6 posiciones
            break;
        default:
            std::cerr << "[Warning] Player has diagonal orientation, using closest cardinal\n";
            rotation = 0;
            break;
    }
    
    // Aplicar rotación a la dirección relativa
    int relativeIndex = static_cast<int>(relativeDir);
    int absoluteIndex = (relativeIndex + rotation) % 8;
    
    return static_cast<Direction>(absoluteIndex);
}

Direction GetOppositeDirection(Direction dir)
{
    switch (dir)
    {
        case Direction::North:      return Direction::South;
        case Direction::NorthEast:  return Direction::SouthWest;
        case Direction::East:       return Direction::West;
        case Direction::SouthEast:  return Direction::NorthWest;
        case Direction::South:      return Direction::North;
        case Direction::SouthWest:  return Direction::NorthEast;
        case Direction::West:       return Direction::East;
        case Direction::NorthWest:  return Direction::SouthEast;
        default:                    return Direction::North;
    }
}