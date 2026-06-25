#include <iostream>
#include <vector>
#include <thread>
#include <chrono>
#include <sstream>
#ifdef _WIN32
#pragma comment(lib, "Ws2_32.lib")
#endif
#include "Connection.h"
#include "Blackboard.h"
#include "IAgent.h"
#include "AgentExplorer.h"
#include "AgentFeeder.h"
#include "AgentChaman.h"
#include "AgentBreeder.h"
#include "AgentStoner.h"
#include "CommandHistory.h"
#include "ClientGame.h"
#include "Result.h"
#include "responses.h"


void WaitForDebugAndClean(int seconds = 5)
{
	std::cout << "\n[DEBUG] Waiting " << seconds << " seconds before closing..." << std::endl;
	std::this_thread::sleep_for(std::chrono::seconds(seconds));
#ifdef _WIN32
	WSACleanup();
	ClientGame::Dispose();
#endif
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
	agents.push_back(new AgentFeeder());
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
	Connection* conn = ClientGame::GetInstance()->connection;
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
	board->Me.TeamName = teamName;
	board->InitializeMap(x, y);

	return Result<Blackboard*>::Success(board);
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
	ClientGame::GetInstance()->connection = conn;


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
	ClientGame::GetInstance()->blackboard = result.Value;
	Blackboard& board = *result.Value;

	std::vector<IAgent*> agents;
	CreateAgents(agents);
	//int i = 0;

	while (true)
	{
		board.Bids.clear();
		for (auto& agent : agents)
			agent->GetBids(board);

		Bid* bestBid = GetBestBid(board.Bids);
		if (!bestBid)
			continue;

		std::string commandStr = CommandTypeToString(bestBid->Command.type);
		if (!bestBid->Command.commandParameter.empty())
			commandStr += " " + bestBid->Command.commandParameter;

		board.commandHistory.AddCommand(bestBid->Command.type, board.CurrentTick, bestBid->Command.commandParameter);
		std::cout << "[Client] CMD => " << commandStr << "\n";
		if (!conn->SendLine(commandStr))
			break;



		////test
		//std::string testCommand;
		//std::string objectParam = "";

		//if (i == 0)
		//{
		//	testCommand = "avance";
		//	std::cout << "[Client] CMD => avance (iteration " << i << ")\n";
		//}
		//else
		//{
		//	testCommand = "prend nourriture";
		//	objectParam = "nourriture";
		//	std::cout << "[Client] CMD => prend nourriture (iteration " << i << ")\n";
		//}

		//board.commandHistory.AddCommand(ParseCommandType(testCommand), board.CurrentTick, objectParam);
		//i++;

		//if (!conn->SendLine(testCommand))
		//	break;

			/////


		std::string response;
		int responseCode;
		while (true)
		{
			if (!conn->RecvLine(response))
				break;

			std::cout << "[Server] RESP <= " << response << "\n";
			responseCode = handleServerResponse(board, response);
			if (responseCode == 0)
				break;
			else if (responseCode == -1)
			{
				WaitForDebugAndClean();
				ClientGame::Dispose();
				return 0;
			}

		}

		std::this_thread::sleep_for(std::chrono::seconds(5));
	}

	// Legacy mode maybe of use is clusters.
    for (auto* a : agents)
		{
			delete a;
		}
	// No legacy make this way
	//std::vector<std::unique_ptr<IAgent>> agents;
	//agents.push_back(std::make_unique<AgentBreeder>());
	//and STL will call the virtual destructor on each Iagent when is called here destructor.



	// Liberar recursos registrados en ClientGame
	ClientGame::Dispose();
#ifdef _WIN32
    WSACleanup();
#endif
    return 0;
}
