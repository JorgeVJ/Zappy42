#include <iostream>
#include <vector>
#include <thread>
#include <chrono>
#include <sstream>

#pragma comment(lib, "Ws2_32.lib")

#include "Connection.h"
#include "Blackboard.h"
#include "IAgent.h"
#include "AgentExplorer.h"
#include "AgentHungry.h"
#include "AgentChaman.h"
#include "AgentBreeder.h"
#include "AgentStoner.h"
#include "CommandHistory.h"
#include "ClientGame.h"
#include "Result.h"

void WaitForDebugAndClean(int seconds = 5)
{
	std::cout << "\n[DEBUG] Waiting " << seconds << " seconds before closing..." << std::endl;
	std::this_thread::sleep_for(std::chrono::seconds(seconds));
	WSACleanup();
}

Bid* GetBestBid(std::vector<Bid>& bids)
{
	if (bids.empty())
		return nullptr;

	Bid* best = &bids[0];
	for (auto& bid : bids)
		if (bid.Value > best->Value)
			best = &bid;

	return best;
}

void CreateAgents(std::vector<IAgent*>& agents)
{
	agents.push_back(new AgentExplorer());
	agents.push_back(new AgentHungry());
	agents.push_back(new AgentChaman());
	agents.push_back(new AgentBreeder());
	agents.push_back(new AgentStoner());
}

// Ahora InitServerHandshake devuelve Result<Blackboard*>
// Usa la Connection ya registrada en ClientGame para el recv/send durante el handshake.
// Si el handshake es correcto crea y devuelve un Blackboard* inicializado.
Result<Blackboard*> InitServerHandshake(const std::string& teamName)
{
	std::string line;
	Connection* conn = ClientGame::GetInstance()->Connection;
	if (!conn || !conn->IsValid())
		return Result<Blackboard*>::Fail("No connection available");

	// Esperar BIENVENUE
	if (!conn->RecvLine(line))
		return Result<Blackboard*>::Fail("Server closed before BIENVENUE");
	if (line != "BIENVENUE")
		return Result<Blackboard*>::Fail(std::string("Server message error: expected 'BIENVENUE', got '") + line + "'");

	std::cout << "[Server] " << line << std::endl;

	if (!conn->SendLine(teamName))
		return Result<Blackboard*>::Fail("Failed to send team name");

	if (!conn->RecvLine(line))
		return Result<Blackboard*>::Fail("Failed to receive nb-client");

	int nb_client = 0;
	try
	{
		size_t pos = 0;
		nb_client = std::stoi(line, &pos);
		if (pos != line.length())
			return Result<Blackboard*>::Fail("Invalid nb-client format: extra characters");
	}
	catch (...)
	{
		return Result<Blackboard*>::Fail(std::string("Invalid nb-client: '") + line + "'");
	}

	if (nb_client < 1)
	{
		// equipo lleno -> no error t�cnico, pero queremos notificar que no se puede unir
		return Result<Blackboard*>::Fail("TeamFull");
	}

	std::cout << "[Server] Available slots: " << nb_client << std::endl;

	if (!conn->RecvLine(line))
		return Result<Blackboard*>::Fail("Failed to receive map dimensions");

	if (line.empty())
		return Result<Blackboard*>::Fail("Received empty map dimensions");

	int x = 0, y = 0;
	std::stringstream ss(line);
	if (!(ss >> x >> y))
		return Result<Blackboard*>::Fail(std::string("Error: Failed to parse X and Y from line: '") + line + "'");

	if (x <= 0 || y <= 0)
		return Result<Blackboard*>::Fail("Error: Invalid map dimensions");

	std::string extra;
	if (ss >> extra)
		std::cerr << "Warning: Extra parameters in map dimensions: '" << extra << "'\n";

	std::cout << "[Server] Map dimensions: X=" << x << ", Y=" << y << "\n";

	// Crear y configurar Blackboard usando la Connection ya registrada en ClientGame
	Blackboard* board = new Blackboard();
	board->Sock = conn;
	board->Me.TeamName = teamName;
	board->InitializeMap(x, y);

	return Result<Blackboard*>::Success(board);
}

void handleServerResponse(Blackboard& board, const std::string& response)
{
	// Obtener el ultimo comando enviado
	const auto& pendingCommands = board.commandHistory.GetPendingCommands();
	if (pendingCommands.empty())
	{
		// No hay comandos pendientes, solo guardar el mensaje. Guardar cuando se recibio?
		MessageEntry msg;
		msg.Message = response;
		msg.MarcoPolo = false;
		msg.From = -1;
		msg.Tick = board.CurrentTick;
		board.Messages.push_back(msg);
		return;
	}

	CommandType lastCommand = pendingCommands.front().type;

	// Parsear tipo de respuesta
	if (response == "ok")
	{
		switch (lastCommand)
		{
		case CommandType::Advance:
			std::cout << "[Action] Moved forward successfully\n";
			// Actualizar posicion en el Blackboard
			break;
		case CommandType::Right:
			std::cout << "[Action] Turned right successfully\n";
			// Actualizar orientacion en el Blackboard
			break;
		case CommandType::Left:
			std::cout << "[Action] Turned left successfully\n";
			// Actualizar orientacion en el Blackboard
			break;
		case CommandType::Take:
			std::cout << "[Action] Took object successfully\n";
			// Actualizar inventario en el Blackboard
			break;
		case CommandType::Put:
			std::cout << "[Action] Put object successfully\n";
			// Actualizar inventario en el Blackboard
			break;
		case CommandType::Expulse:
			std::cout << "[Action] Expelled player successfully\n";
			break;
		case CommandType::Fork:
			std::cout << "[Action] Fork successful\n";
			break;
		default:
			std::cout << "[Action] Command executed successfully\n";
			break;
		}
	}
	else if (response == "ko")
	{
		switch (lastCommand)
		{
		case CommandType::Advance:
			std::cout << "[Action] Failed to move forward\n";
			break;
		case CommandType::Take:
			std::cout << "[Action] Failed to take object (not present)\n";
			break;
		case CommandType::Put:
			std::cout << "[Action] Failed to put object (not in inventory)\n";
			break;
		case CommandType::Incantation:
			std::cout << "[Action] Incantation failed\n";
			break;
		default:
			std::cout << "[Action] Command failed\n";
			break;
		}
	}
	else if (response.find("elevation en cours") != std::string::npos)
	{
		if (lastCommand == CommandType::Incantation)
		{
			std::cout << "[Action] Incantation in progress...\n";
			// Marcar estado de elevacion en el Blackboard
		}
	}
	else if (response.find('{') != std::string::npos && response.find('}') != std::string::npos)
	{
		// Respuesta con datos (JSON-like o estructura de datos)
		switch (lastCommand)
		{
		case CommandType::See:
			std::cout << "[Action] Processing vision data\n";
			board.HandleVoirResponse(response);
			break;
		case CommandType::Inventory:
			std::cout << "[Action] Processing inventory data\n";
			// Parsear y actualizar inventario en el Blackboard
			break;
		case CommandType::ConnectNbr:
			std::cout << "[Action] Processing connection number\n";
			// Procesar numero de conexiones disponibles
			break;
		default:
			std::cout << "[Action] Received structured data\n";
			{
				MessageEntry msg;
				msg.Message = response;
				msg.MarcoPolo = false;
				msg.From = -1;
				msg.Tick = board.CurrentTick;
				board.Messages.push_back(msg);
			}
			break;
		}
	}
	else
	{
		// Otras respuestas (por ejemplo, mensajes de broadcast, muerte, etc.)
		if (lastCommand == CommandType::Broadcast)
		{
			std::cout << "[Action] Broadcast sent\n";
		}
		else if (response.find("mort") != std::string::npos)
		{
			std::cout << "[Action] Player died\n";
			// Manejar muerte del jugador
		}

		MessageEntry msg;
		msg.Message = response;
		msg.MarcoPolo = false;
		msg.From = -1;
		msg.Tick = board.CurrentTick;
		board.Messages.push_back(msg);
	}
}

int main()
{
	//TODO parsear argumentos de linea de comando para ip, puerto y nombre de equipo
	// Crear Connection y registrarla en ClientGame
#ifdef _WIN32
	WSADATA wsaData{};
	WSAStartup(MAKEWORD(2, 2), &wsaData);
#endif
    SOCKET rawSock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	Connection* conn = new Connection(rawSock);
	ClientGame::GetInstance()->Connection = conn;


    sockaddr_in serverAddr{};
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(12345);
#ifdef _WIN32
    inet_pton(AF_INET, "127.0.0.1", &serverAddr.sin_addr);
#elif defined(__linux__)
	serverAddr.sin_addr.s_addr = inet_addr("127.0.0.1");
#endif
	if (connect(conn->Get(), (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
	{
		std::cerr << "connect() failed\n";
		WaitForDebugAndClean();
		return 1;
	}

	// Handshake: InitServerHandshake crear� el Blackboard e internamente usar� la Connection en ClientGame
	auto result = InitServerHandshake("TestTeamName");
	if (!result.Ok)
	{
		if (result.Message == "TeamFull")
		{
			std::cout << "Team is full. Disconnecting...\n";
			WaitForDebugAndClean();
			ClientGame::Dispose();
			return 0;
		}
		std::cerr << "Server handshake failed: " << result.Message << "\n";
		WaitForDebugAndClean();
		ClientGame::Dispose();
		return 1;
	}

	// Registrar Blackboard en ClientGame y mantener referencia local
	ClientGame::GetInstance()->Blackboard = result.Value;
	Blackboard& board = *result.Value;

	std::vector<IAgent*> agents;
	CreateAgents(agents);

	while (true)
	{
		board.Bids.clear();
		for (auto& agent : agents)
			agent->GetBids(board);

		Bid* bestBid = GetBestBid(board.Bids);
		if (!bestBid)
			continue;

		board.commandHistory.AddCommand(bestBid->Type, board.CurrentTick, "");
		std::cout << "[Client] CMD => " << bestBid->Command << "\n";

		if (!board.Sock->SendLine(bestBid->Command))
			break;

		std::string response;
		while (true)
		{
			if (!board.Sock->RecvLine(response))
				break;

			std::cout << "[Server] RESP <= " << response << "\n";
			handleServerResponse(board, response);
		}

		std::this_thread::sleep_for(std::chrono::seconds(10));
	}

	// Legacy mode maybe of use is clusters.
    for (auto* a : agents)
        delete a;
	// No legacy make this way
	//std::vector<std::unique_ptr<IAgent>> agents;
	//agents.push_back(std::make_unique<AgentBreeder>());
	//and STL will call the virtual destructor on each Iagent when is called here destructor.



	// Liberar recursos registrados en ClientGame
	ClientGame::Dispose();

	WSACleanup();
	return 0;
#ifdef _WIN32
    WSACleanup();
#endif
    return 0;
}
