#pragma once
#include <string>

// Forward declaration
class Blackboard;

/// <summary>
/// Maneja la respuesta del servidor según el último comando enviado
/// </summary>
/// <param name="board">Referencia al Blackboard con el estado del juego</param>
/// <param name="response">Respuesta recibida del servidor</param>
/// <returns>0 si se procesó correctamente, 1 si hubo un error o comando no manejado</returns>
int handleServerResponse(Blackboard& board, const std::string& response);