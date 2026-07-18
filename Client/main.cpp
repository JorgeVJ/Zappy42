#include <iostream>
#include <vector>
#include <thread>
#include <chrono>
#include <sstream>
#include <span>
#include <stdexcept>
#ifdef _WIN32
#pragma comment(lib, "Ws2_32.lib")
#elif defined(__linux__)
# include <netdb.h>
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
#include "GetOpt.h"

// Especificacion de los argumentos del cliente: -n <team> -p <port> [-h <hostname>]
namespace Opt {
	namespace ClientArgs {
		enum class Id {
			Port,
			Teams,
			Host,
		};

		constexpr Spec specs[] = {
			{ port_keys,  Arity::One,       RepeatPolicy::Reject },
			{ teams_keys, Arity::One,       RepeatPolicy::Reject },
			{ host_keys,  Arity::ZeroOrOne, RepeatPolicy::Reject },
		};

		const KeyEntry<Id> key_table[] = {
			{ "-p", Id::Port },
			{ "-n", Id::Teams },
			{ "-h", Id::Host },
		};
	}
}

struct ClientOptions
{
	std::string teamName;
	std::string host = "localhost";
	std::string portStr;
	int port = 0;
};

static void PrintUsage()
{
	std::cerr << "Usage: ./client -n <team> -p <port> [-h <hostname>]\n";
}

// Parsea argv y rellena 'out'. Devuelve false (e imprime el error) si es invalido.
static bool ParseClientArgs(int argc, char** argv, ClientOptions& out)
{
	using namespace Opt::ClientArgs;

	std::vector<std::string> errors;
	Opt::GetOpt<Id> opts(std::span{ specs }, std::span{ key_table });

	bool ok = opts.parse(argc, argv, &errors);
	ok &= validate_arity(opts.values, opts.specs, &errors);

	if (!ok || !errors.empty())
	{
		for (const auto& e : errors)
			std::cerr << e << std::endl;
		PrintUsage();
		return false;
	}

	out.teamName = std::string(opts.values[static_cast<size_t>(Id::Teams)].values[0]);
	out.portStr  = std::string(opts.values[static_cast<size_t>(Id::Port)].values[0]);

	const auto& hostVal = opts.values[static_cast<size_t>(Id::Host)];
	if (hostVal.present && !hostVal.values.empty())
		out.host = std::string(hostVal.values[0]);

	if (out.teamName.empty())
	{
		std::cerr << "Team name (-n) cannot be empty\n";
		return false;
	}

	try
	{
		size_t pos = 0;
		out.port = std::stoi(out.portStr, &pos);
		if (pos != out.portStr.size())
		{
			std::cerr << "Invalid port: '" << out.portStr << "'\n";
			return false;
		}
	}
	catch (...)
	{
		std::cerr << "Invalid port: '" << out.portStr << "'\n";
		return false;
	}

	if (out.port < 1 || out.port > 65535)
	{
		std::cerr << "Port out of range (1-65535): " << out.portStr << "\n";
		return false;
	}

	return true;
}

// Resuelve host:port y devuelve un socket ya conectado (INVALID_SOCKET si falla).
static SOCKET ConnectToServer(const std::string& host, const std::string& portStr)
{
	struct addrinfo hints{};
	hints.ai_family = AF_INET;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_protocol = IPPROTO_TCP;

	struct addrinfo* res = nullptr;
	int gai = getaddrinfo(host.c_str(), portStr.c_str(), &hints, &res);
	if (gai != 0 || !res)
	{
#ifdef _WIN32
		// En Windows gai_strerror puede resolver a la variante wchar_t*; usamos el codigo numerico.
		std::cerr << "Could not resolve host '" << host << "' (getaddrinfo error " << gai << ")\n";
#else
		std::cerr << "Could not resolve host '" << host << "': " << gai_strerror(gai) << "\n";
#endif
		return INVALID_SOCKET;
	}

	SOCKET sock = INVALID_SOCKET;
	for (struct addrinfo* ai = res; ai != nullptr; ai = ai->ai_next)
	{
		sock = socket(ai->ai_family, ai->ai_socktype, ai->ai_protocol);
		if (sock == INVALID_SOCKET)
			continue;

		if (connect(sock, ai->ai_addr, static_cast<int>(ai->ai_addrlen)) != SOCKET_ERROR)
			break;

#ifdef _WIN32
		closesocket(sock);
#else
		close(sock);
#endif
		sock = INVALID_SOCKET;
	}

	freeaddrinfo(res);
	return sock;
}


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
		// equipo lleno -> no error tï¿½cnico, pero queremos notificar que no se puede unir
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



int main(int argc, char** argv)
{
	// Parsear argumentos de linea de comando: -n <team> -p <port> [-h <hostname>]
	ClientOptions options;
	if (!ParseClientArgs(argc, argv, options))
		return 1;

	std::cout << "[Client] Team: " << options.teamName
			  << " | Host: " << options.host
			  << " | Port: " << options.port << "\n";

#ifdef _WIN32
	WSADATA wsaData{};
	if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
	{
		std::cerr << "WSAStartup() failed\n";
		return 1;
	}
#endif

	SOCKET rawSock = ConnectToServer(options.host, options.portStr);
	if (rawSock == INVALID_SOCKET)
	{
		std::cerr << "connect() failed to " << options.host << ":" << options.port << "\n";
#ifdef _WIN32
		WSACleanup();
#endif
		return 1;
	}

	// Crear Connection y registrarla en ClientGame
	Connection* conn = new Connection(rawSock);
	ClientGame::GetInstance()->connection = conn;

	// Handshake: InitServerHandshake creara el Blackboard e internamente usara la Connection en ClientGame
	auto result = InitServerHandshake(options.teamName);
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
