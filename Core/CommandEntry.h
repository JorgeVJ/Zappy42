#pragma once
#include "CommandType.h"

struct CommandEntry
{
    CommandType type;
    std::string commandParameter;
    long startTick;
    long endTick;

    /// <summary>
    /// Crea un CommandEntry con los ticks calculados automaticamente
    /// </summary>
    /// <param name="cmdType">Tipo de comando</param>
    /// <param name="param">Parametro del comando (vacio si no tiene)</param>
    /// <param name="currentTick">Tick actual del juego</param>
    /// <returns>CommandEntry inicializado</returns>
    static CommandEntry Create(CommandType cmdType, const std::string& param, long currentTick);
    
    /// <summary>
    /// Sobrecarga para comandos sin parametros
    /// </summary>
    static CommandEntry Create(CommandType cmdType, long currentTick);
};
