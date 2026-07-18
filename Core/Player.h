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

    // Constructor por defecto
    Player() = default;

    /// <summary>
    /// Mueve al jugador en la direcci�n en la que est� mirando
    /// </summary>
    /// <param name="steps">N�mero de pasos (positivo avanza, negativo retrocede)</param>
    void Move(int steps = 1);

    /// <summary>
    /// Mueve al jugador en una direcci�n espec�fica (soporta diagonales)
    /// </summary>
    /// <param name="dir">Direcci�n del movimiento</param>
    /// <param name="steps">N�mero de pasos</param>
    void MoveInDirection(Direction dir, int steps = 1);

    /// <summary>
    /// Gira al jugador en la direcci�n especificada
    /// </summary>
    /// <param name="turnDir">Direcci�n del giro (Right o Left)</param>
    void Turn(TurnDirection turnDir);

    /// <summary>
    /// Obtiene la direcci�n opuesta a la orientaci�n actual
    /// </summary>
    Direction GetOppositeDirection() const;

private:
    /// <summary>
    /// Convierte la orientaci�n actual a un �ndice (0-3)
    /// </summary>
    int GetDirectionIndex() const;

    /// <summary>
    /// Convierte un �ndice (0-3) a una direcci�n cardinal
    /// </summary>
    Direction IndexToDirection(int index) const;
};
