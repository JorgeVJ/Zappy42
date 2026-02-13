#include "pch.h"
#include "Player.h"
#include <iostream>

void Player::Move(int steps)
{
    switch (Orientation)
    {
        case Direction::North:
            Position.Y += steps;
            break;
        case Direction::East:
            Position.X += steps;
            break;
        case Direction::South:
            Position.Y -= steps;
            break;
        case Direction::West:
            Position.X -= steps;
            break;
        default:
            // Si hay una orientación diagonal, no se mueve
			std::cout << "Warning: Player has diagonal orientation, Move() does not support it. No movement applied.\n";
            break;
    }
}

void Player::Turn(TurnDirection turnDir)
{
    int currentIndex = GetDirectionIndex();
    
    if (turnDir == TurnDirection::Right)
    {
        // Giro a la derecha: North(0) -> East(1) -> South(2) -> West(3) -> North(0)
        currentIndex = (currentIndex + 1) % 4;
    }
    else // TurnDirection::Left
    {
        // Giro a la izquierda: North(0) -> West(3) -> South(2) -> East(1) -> North(0)
        currentIndex = (currentIndex - 1 + 4) % 4;
    }
    
    Orientation = IndexToDirection(currentIndex);
}

Direction Player::GetOppositeDirection() const
{
    switch (Orientation)
    {
        case Direction::North:
            return Direction::South;
        case Direction::East:
            return Direction::West;
        case Direction::South:
            return Direction::North;
        case Direction::West:
            return Direction::East;
        default:
            return Orientation;
    }
}

int Player::GetDirectionIndex() const
{
    switch (Orientation)
    {
        case Direction::North:
            return 0;
        case Direction::East:
            return 1;
        case Direction::South:
            return 2;
        case Direction::West:
            return 3;
        default:
            return 0; // Por defecto Norte
    }
}

Direction Player::IndexToDirection(int index) const
{
    switch (index)
    {
        case 0:
            return Direction::North;
        case 1:
            return Direction::East;
        case 2:
            return Direction::South;
        case 3:
            return Direction::West;
        default:
            return Direction::North;
    }
}