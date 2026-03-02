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
    
    // Food consumption tracking
    int LastFoodUpdateTick = 0;

    // Constructor por defecto
    Player() = default;

    /// <summary>
    /// Actualiza el inventario de comida consumiendo 1 unidad cada 126 ticks
    /// </summary>
    /// <param name="currentTick">Tick actual del juego</param>
    /// <returns>Cantidad de comida consumida en esta actualizacion</returns>
    int UpdateFoodConsumption(int currentTick);

    /// <summary>
    /// Mueve al jugador en la direccion en la que esta mirando
    /// </summary>
    /// <param name="steps">Numero de pasos (positivo avanza, negativo retrocede)</param>
    void Move(int steps = 1);

    /// <summary>
    /// Mueve al jugador en una direccion especifica (soporta diagonales)
    /// </summary>
    /// <param name="dir">Direccion del movimiento</param>
    /// <param name="steps">Numero de pasos</param>
    void MoveInDirection(Direction dir, int steps = 1);

    /// <summary>
    /// Gira al jugador en la direccion especificada
    /// </summary>
    /// <param name="turnDir">Direccion del giro (Right o Left)</param>
    void Turn(TurnDirection turnDir);

    /// <summary>
    /// Obtiene la direccion opuesta a la orientacion actual
    /// </summary>
    Direction GetOppositeDirection() const;

private:
    /// <summary>
    /// Convierte la orientacion actual a un indice (0-3)
    /// </summary>
    int GetDirectionIndex() const;

    /// <summary>
    /// Convierte un indice (0-3) a una direccion cardinal
    /// </summary>
    Direction IndexToDirection(int index) const;
};