#include "responses.h"
#include "Blackboard.h"
#include "CommandType.h"
#include "MessageEntry.h"
#include "Direction.h"
#include "ClientLog.h"
#include <string>
#include <sstream>

int handleServerResponse(Blackboard& board, const std::string& response)
{
	// Obtener el ultimo comando enviado
	const auto& pendingCommands = board.commandHistory.GetPendingCommands();
	CommandEntry lastCommand;
	if (pendingCommands.empty())
		lastCommand = CommandEntry::Create(CommandType::Empty, board.CurrentTick);
	else
		lastCommand = pendingCommands.front();
	// Tipo de respuesta
	if (response == "ok")
	{
		switch (lastCommand.type)
		{
		case CommandType::Advance:
			board.Me.Move(1);
			board.UpdateTick(GetCommandDuration(CommandType::Advance));
			if (board.ExplorerHasMovementPlan && board.Me.Orientation == board.ExplorerTargetDirection)
				board.ExplorerHasMovementPlan = false;
			LOG_ACTION("Moved forward successfully");
			LOG_PLAYER("Position: (" << board.Me.Position.X << ", " << board.Me.Position.Y
				<< ") Direction: " << DirectionToString(board.Me.Orientation)
				<< " (" << DirectionToInt(board.Me.Orientation) << ")");
			return 0;
			break;
		case CommandType::Right:
			board.Me.Turn(TurnDirection::Right);
			board.UpdateTick(GetCommandDuration(CommandType::Right));
			LOG_ACTION("Turned right successfully");
			LOG_PLAYER("Now facing: " << DirectionToString(board.Me.Orientation));
			return 0;
			break;
		case CommandType::Left:
			board.Me.Turn(TurnDirection::Left);
			board.UpdateTick(GetCommandDuration(CommandType::Left));
			LOG_ACTION("Turned left successfully");
			LOG_PLAYER("Now facing: " << DirectionToString(board.Me.Orientation));
			return 0;
			break;
		case CommandType::Take:
			board.Me.inventory.Add(lastCommand.commandParameter, 1);
			board.UpdateTick(GetCommandDuration(CommandType::Take));
			LOG_ACTION("Took " << lastCommand.commandParameter << " successfully");
			board.Me.inventory.Print("Updated Inventory");
			return 0;
			break;
		case CommandType::Put:
			board.Me.inventory.Remove(lastCommand.commandParameter, 1);
			board.UpdateTick(GetCommandDuration(CommandType::Put));
			LOG_ACTION("Put " << lastCommand.commandParameter << " successfully");
			board.Me.inventory.Print("Updated Inventory");
			return 0;
			break;
		case CommandType::Expulse:
			board.UpdateTick(GetCommandDuration(CommandType::Expulse));
			LOG_ACTION("Expelled players successfully");
			return 0;
			break;
		case CommandType::Broadcast:
			board.UpdateTick(GetCommandDuration(CommandType::Broadcast));
			LOG_ACTION("Message broadcasted successfully");
			// Check if chaman is in Marco o Polo mode.
			// Check last messages or keep a count of message sent to change strategy
			return 0;
			break;
		case CommandType::Fork:
			board.UpdateTick(GetCommandDuration(CommandType::Fork));
			// Update on chaman to start call partners?
			// Check for Team available connections? Or try to level up with others?
			LOG_ACTION("Fork successful");
			board.TeamNbr += 1;
			return 0;
			break;
		default:
			LOG_WARNING("Undefined LastCommand " << CommandTypeToString(lastCommand.type));
			return 1;
			break;
		}
	}
	else if (response == "ko")
	{
		LOG_WARNING("Failed to execute " << CommandTypeToString(lastCommand.type));
		return 0;
	}
	else if (response.find('{') != std::string::npos && response.find('}') != std::string::npos)
	{
		// Respuesta con datos (JSON-like o estructura de datos)
		switch (lastCommand.type)
		{
		case CommandType::See:
			board.HandleVoirResponse(response);
			board.LastSeeTick = board.CurrentTick;
			board.UpdateTick(GetCommandDuration(CommandType::See));
			LOG_ACTION("Processing vision data");
			return 0;
			break;
		case CommandType::Inventory:
			board.Me.inventory.SetFromServerString(response);
			board.UpdateTick(GetCommandDuration(CommandType::Inventory));
			LOG_ACTION("Inventory data updated.");
			board.Me.inventory.Print("Player Inventory");
			return 0;
			break;
		default:
			LOG_WARNING("Undefined LastCommand: " << CommandTypeToString(lastCommand.type) << ". Received structured response: " << response);
			return 1;
			break;
		}
	}
	else if (response.find("elevation en cours") != std::string::npos)
	{
		if (lastCommand.type == CommandType::Incantation)
		{
			LOG_ACTION("Incantation in progress...");
			// Se queda esperando respuesta de level up
			return 1;
		}
	}
	else if (response.find("niveau actuel") != std::string::npos)
	{
		if (lastCommand.type == CommandType::Incantation)
		{
			LOG_ACTION("Incantation completed!");
			if (board.HandleIncantationResponse(response))
			{
				board.UpdateTick(GetCommandDuration(CommandType::Incantation));
				board.Me.inventory.Print("Inventory after Level Up");
				return 0;
			}
			else
			{
				LOG_ERROR("Failed to process incantation response");
				return 1;
			}
			return 0;
		}
	}
	else if (response.find("deplacement") != std::string::npos)
	{
		// receive expulse command from other client
		std::stringstream ss(response);
		std::string keyword;
		int soundNumber;
		
		ss >> keyword >> soundNumber;
		
		if (keyword != "deplacement" || ss.fail())
		{
			LOG_ERROR("Failed to parse deplacement response: '" << response << "'");
			return 1;
		}
		
		if (soundNumber < 0 || soundNumber > 8)
		{
			LOG_ERROR("Invalid deplacement number: " << soundNumber);
			return 1;
		}
		
		// Obtener la direccion DESDE donde viene el sonido/empujon
		Direction soundFromDirection = BroadcastNumberToDirection(soundNumber, board.Me.Orientation);
		
		// Calcular hacia donde nos empujan (direccion opuesta)
		Direction pushToDirection = GetOppositeDirection(soundFromDirection);
		
		// Caso especial: si es 0, el expulsor esta en el mismo tile
		if (soundNumber == 0)
		{
			pushToDirection = board.Me.Orientation; // Nos empujan hacia donde estamos mirando
			
		}
		// Guardar posicion anterior
		Point oldPos = board.Me.Position;
		
		// Mover al jugador en la direccion opuesta (puede ser diagonal)
		board.Me.MoveInDirection(pushToDirection, 1);
		
		LOG_ACTION("Expelled by another player!");
		//std::cout << "[Player] Sound direction number: " << soundNumber << " (relative to orientation)\n";
		//std::cout << "[Player] Sound came from: " << DirectionToString(soundFromDirection) << " (absolute)\n";
		//std::cout << "[Player] Pushed towards: " << DirectionToString(pushToDirection) << " (absolute)\n";
		//std::cout << "[Player] Moved from (" << oldPos.X << ", " << oldPos.Y 
		//		  << ") to (" << board.Me.Position.X << ", " << board.Me.Position.Y << ")\n";
		//std::cout << "[Player] Still facing: " << DirectionToString(board.Me.Orientation) << "\n";
		
		return 0;
	}
	else
	{
		if (lastCommand.type == CommandType::Broadcast)
		{
			LOG_DEBUG("Broadcast answer receive. Handle different.");
			return 0;
		}
		else if (lastCommand.type == CommandType::ConnectNbr)
		{
			board.ConnectNbr = std::stoi(response);
			LOG_ACTION("Available connection number: " << std::stoi(response));
			return 0;
		}
		else if (response.find("mort") != std::string::npos)
		{
			LOG_ACTION("Player died");
			// Manejar muerte del jugador
			return -1;
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
			LOG_WARNING("Unhandled server response: " << response);
			return 1;
		}
	}

	return 0;
}