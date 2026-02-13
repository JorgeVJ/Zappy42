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
    Inventory Inventory;
    std::string TeamName = "";

    // Constructor por defecto
    Player() = default;

    /// Mueve al jugador en la dirección en la que está mirando
    /// </summary>
    /// <param name="steps">Número de pasos (positivo avanza, negativo retrocede)</param>
    void Move(int steps = 1);

    /// <summary>
    /// Mueve al jugador en una dirección específica (soporta diagonales)
    /// </summary>
    /// <param name="dir">Dirección del movimiento</param>
    /// <param name="steps">Número de pasos</param>
    void MoveInDirection(Direction dir, int steps = 1);

    /// <summary>
    /// Gira al jugador en la dirección especificada
    /// </summary>
    /// <param name="turnDir">Dirección del giro (Right o Left)</param>
    void Turn(TurnDirection turnDir);

    /// <summary>
    /// Obtiene la dirección opuesta a la orientación actual
    /// </summary>
    Direction GetOppositeDirection() const;

private:
    /// <summary>
    /// Convierte la orientación actual a un índice (0-3)
    /// </summary>
    int GetDirectionIndex() const;

    /// <summary>
    /// Convierte un índice (0-3) a una dirección cardinal
    /// </summary>
    Direction IndexToDirection(int index) const;
};