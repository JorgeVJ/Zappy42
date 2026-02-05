#include "responses.h"
#include "Blackboard.h"
#include "CommandType.h"
#include "MessageEntry.h"
#include "Direction.h"
#include <iostream>
#include <string>

int handleServerResponse(Blackboard& board, const std::string& response)
{
	// Obtener el ultimo comando enviado
	const auto& pendingCommands = board.commandHistory.GetPendingCommands();
	CommandType lastCommand = pendingCommands.front().type;

	// Tipo de respuesta
	if (response == "ok")
	{
		switch (lastCommand)
		{
		case CommandType::Advance:
			board.Me.Move(1);
			std::cout << "[Action] Moved forward successfully\n";
			std::cout << "[Player] Position: (" << board.Me.Position.X << ", " << board.Me.Position.Y
				<< ") Direction: " << DirectionToString(board.Me.Orientation)
				<< " (" << DirectionToInt(board.Me.Orientation) << ")\n";
			return 0;
			break;
		case CommandType::Right:
			board.Me.Turn(TurnDirection::Right);
			std::cout << "[Action] Turned right successfully\n";
			std::cout << "[Player] Now facing: " << DirectionToString(board.Me.Orientation) << "\n";
			return 0;
			break;
		case CommandType::Left:
			board.Me.Turn(TurnDirection::Left);
			std::cout << "[Action] Turned left successfully\n";
			std::cout << "[Player] Now facing: " << DirectionToString(board.Me.Orientation) << "\n";
			return 0;
			break;
		case CommandType::Take:
			std::cout << "[Action] Took object successfully\n";
			// Actualizar inventario en el Blackboard
			return 0;
			break;
		case CommandType::Put:
			std::cout << "[Action] Put object successfully\n";
			// Actualizar inventario en el Blackboard
			return 0;
			break;
		case CommandType::Expulse:
			std::cout << "[Action] Expelled player successfully\n";
			return 0;
			break;
		case CommandType::Fork:
			std::cout << "[Action] Fork successful\n";
			return 0;
		default:
			std::cout << "[Action] Undefined LastCommand " << CommandTypeToString(lastCommand) << "\n";
			return 1;
			break;
		}
	}
	else if (response == "ko")
	{
		std::cout << "[Action] Failed to execute " << CommandTypeToString(lastCommand) << "\n";
		return 0;
		/*switch (lastCommand)
		{
		case CommandType::Advance:
			std::cout << "[Action] Failed to move forward\n";
			return 0;
			break;
		case CommandType::Take:
			std::cout << "[Action] Failed to take object (not present)\n";
			return 0;
			break;
		case CommandType::Put:
			std::cout << "[Action] Failed to put object (not in inventory)\n";
			return 0;
			break;
		case CommandType::Incantation:
			std::cout << "[Action] Incantation failed\n";
			return 0;
			break;
		default:
			std::cout << "[Action] Undefined LastCommand failed " << CommandTypeToString(lastCommand) << "\n";
			return 1;
			break;
		}*/
	}
	else if (response.find('{') != std::string::npos && response.find('}') != std::string::npos)
	{
		// Respuesta con datos (JSON-like o estructura de datos)
		switch (lastCommand)
		{
		case CommandType::See:
			std::cout << "[Action] Processing vision data\n";
			board.HandleVoirResponse(response);
			return 0;
			break;
		case CommandType::Inventory:
			std::cout << "[Action] Processing inventory data\n";
			// Parsear y actualizar inventario en el Blackboard
			return 0;
			break;
		default:
			std::cout << "[Action] Undefined LastCommand: " << CommandTypeToString(lastCommand) << ". Received structured response: " << response << "\n";
			return 1;
			break;
		}
	}
	else if (response.find("elevation en cours") != std::string::npos)
	{
		if (lastCommand == CommandType::Incantation)
		{
			std::cout << "[Action] Incantation in progress...\n";
			// Se queda esperando respuesta de level up
			return 1;
		}
	}
	else if (response.find("niveau actuel") != std::string::npos)
	{
		if (lastCommand == CommandType::Incantation)
		{
			std::cout << "[Action] Incantation has finished. Level: " << board.Me.Level << "\n";
			// Marcar estado de elevacion en el Blackboard
			return 0;
		}
	}
	else
	{
		if (lastCommand == CommandType::Broadcast)
		{
			std::cout << "[Action] Broadcast answer receive. Handle different. \n";
			return 0;
		}
		else if (lastCommand == CommandType::ConnectNbr)
		{
			std::cout << "[Action] Processing connection number\n";
			// Procesar número de conexiones disponibles
			return 0;
		}
		else if (response.find("mort") != std::string::npos)
		{
			std::cout << "[Action] Player died\n";
			// Manejar muerte del jugador
			return 0;
		}
		else if (response.find("message") != std::string::npos)
		{
			// Parse message to save in struct. Save current tick.
			MessageEntry msg;
			msg.Message = response;
			msg.MarcoPolo = false;
			msg.From = -1;
			msg.Tick = board.CurrentTick;
			board.Messages.push_back(msg);
			return 1;
		}
		else
		{
			// Respuesta no reconocida
			std::cout << "[Warning] Unhandled server response: " << response << "\n";
			return 1;
		}
	}

	return 0;
}