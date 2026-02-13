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
	CommandEntry lastCommand = pendingCommands.front();

	// Tipo de respuesta
	if (response == "ok")
	{
		switch (lastCommand.type)
		{
		case CommandType::Advance:
			board.Me.Move(1);
			board.UpdateTick(7);
			std::cout << "[Action] Moved forward successfully\n";
			std::cout << "[Player] Position: (" << board.Me.Position.X << ", " << board.Me.Position.Y
				<< ") Direction: " << DirectionToString(board.Me.Orientation)
				<< " (" << DirectionToInt(board.Me.Orientation) << ")\n";
			return 0;
			break;
		case CommandType::Right:
			board.Me.Turn(TurnDirection::Right);
			board.UpdateTick(7);
			std::cout << "[Action] Turned right successfully\n";
			std::cout << "[Player] Now facing: " << DirectionToString(board.Me.Orientation) << "\n";
			return 0;
			break;
		case CommandType::Left:
			board.Me.Turn(TurnDirection::Left);
			board.UpdateTick(7);
			std::cout << "[Action] Turned left successfully\n";
			std::cout << "[Player] Now facing: " << DirectionToString(board.Me.Orientation) << "\n";
			return 0;
			break;
		case CommandType::Take:
			board.Me.Inventory.Add(lastCommand.commandParameter, 1);
			board.UpdateTick(7);
			std::cout << "[Action] Took " << lastCommand.commandParameter << " successfully\n";
			board.Me.Inventory.Print("Updated Inventory");
			return 0;
			break;
		case CommandType::Put:
			board.Me.Inventory.Remove(lastCommand.commandParameter, 1);
			board.UpdateTick(7);
			std::cout << "[Action] Put " << lastCommand.commandParameter << " successfully\n";
			board.Me.Inventory.Print("Updated Inventory");
			return 0;
			break;
		case CommandType::Expulse:
			board.UpdateTick(7);
			std::cout << "[Action] Expelled player successfully\n";
			return 0;
			break;
		case CommandType::Broadcast:
			board.UpdateTick(7);
			std::cout << "[Action] Message broadcasted successfully\n";
			return 0;
			break;
		case CommandType::Fork:
			board.UpdateTick(42);
			std::cout << "[Action] Fork successful\n";
			return 0;
			break;
		default:
			std::cout << "[Action] Undefined LastCommand " << CommandTypeToString(lastCommand.type) << "\n";
			return 1;
			break;
		}
	}
	else if (response == "ko")
	{
		std::cout << "[Action] Failed to execute " << CommandTypeToString(lastCommand.type) << "\n";
		return 0;
	}
	else if (response.find('{') != std::string::npos && response.find('}') != std::string::npos)
	{
		// Respuesta con datos (JSON-like o estructura de datos)
		switch (lastCommand.type)
		{
		case CommandType::See:
			board.HandleVoirResponse(response);
			board.UpdateTick(7);
			std::cout << "[Action] Processing vision data\n";
			return 0;
			break;
		case CommandType::Inventory:
			board.Me.Inventory.SetFromServerString(response);
			board.UpdateTick(1);
			std::cout << "[Action] Inventory data updated.\n";
			board.Me.Inventory.Print("Player Inventory");
			return 0;
			break;
		default:
			std::cout << "[Action] Undefined LastCommand: " << CommandTypeToString(lastCommand.type) << ". Received structured response: " << response << "\n";
			return 1;
			break;
		}
	}
	else if (response.find("elevation en cours") != std::string::npos)
	{
		if (lastCommand.type == CommandType::Incantation)
		{
			std::cout << "[Action] Incantation in progress...\n";
			// Se queda esperando respuesta de level up
			return 1;
		}
	}
	else if (response.find("niveau actuel") != std::string::npos)
	{
		if (lastCommand.type == CommandType::Incantation)
		{
			std::cout << "[Action] Incantation completed!\n";
			if (board.HandleIncantationResponse(response))
			{
				board.UpdateTick(300);
				board.Me.Inventory.Print("Inventory after Level Up");
				return 0;
			}
			else
			{
				std::cerr << "[Action] Failed to process incantation response\n";
				return 1;
			}
			return 0;
		}
	}
	else if (response.find("deplacement") != std::string::npos)
	{
		// receive expulse command from other client
		// Parsear "deplacement <K>"
		std::stringstream ss(response);
		std::string keyword;
		int soundNumber;
		
		ss >> keyword >> soundNumber;
		
		if (keyword != "deplacement" || ss.fail())
		{
			std::cerr << "[Error] Failed to parse deplacement response: '" << response << "'\n";
			return 1;
		}
		
		// Validar rango (0-8 según el subject)
		if (soundNumber < 0 || soundNumber > 8)
		{
			std::cerr << "[Error] Invalid deplacement number: " << soundNumber << "\n";
			return 1;
		}
		
		
		
		// Obtener la dirección DESDE donde viene el sonido/empujón
		Direction soundFromDirection = BroadcastNumberToDirection(soundNumber, board.Me.Orientation);
		
		// Calcular hacia dónde nos empujan (dirección opuesta)
		Direction pushToDirection = GetOppositeDirection(soundFromDirection);
		
		// Caso especial: si es 0, el expulsor está en el mismo tile
		if (soundNumber == 0)
		{
			pushToDirection = board.Me.Orientation; // Nos empujan hacia donde estamos mirando
			
		}
		// Guardar posición anterior
		Point oldPos = board.Me.Position;
		
		// Mover al jugador en la dirección opuesta (puede ser diagonal)
		board.Me.MoveInDirection(pushToDirection, 1);
		
		std::cout << "[Action] Expelled by another player!\n";
		std::cout << "[Player] Sound direction number: " << soundNumber << " (relative to orientation)\n";
		std::cout << "[Player] Sound came from: " << DirectionToString(soundFromDirection) << " (absolute)\n";
		std::cout << "[Player] Pushed towards: " << DirectionToString(pushToDirection) << " (absolute)\n";
		std::cout << "[Player] Moved from (" << oldPos.X << ", " << oldPos.Y 
				  << ") to (" << board.Me.Position.X << ", " << board.Me.Position.Y << ")\n";
		std::cout << "[Player] Still facing: " << DirectionToString(board.Me.Orientation) << "\n";
		
		return 0;
	}
	else
	{
		if (lastCommand.type == CommandType::Broadcast)
		{
			std::cout << "[Action] Broadcast answer receive. Handle different. \n";
			return 0;
		}
		else if (lastCommand.type == CommandType::ConnectNbr)
		{
			board.ConnectNbr = std::stoi(response);
			std::cout << "[Action] Available connection number: "<< std::stoi(response) <<"\n";
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