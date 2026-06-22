#include "pch.h"
#include "ServerSimple.h"
#include "responses.h"
#include "events.h"
#include "SocketManager.h"
#include <algorithm>
#include <sstream>

#ifdef _WIN32
#pragma comment(lib, "Ws2_32.lib")
#endif

// ============================================================================
// CONSTRUCTOR & DESTRUCTOR
// ============================================================================

Server::Server(const Opt::Server::Args& args)
	: m_args(args), m_game(nullptr), m_isRunning(false)
{
}

Server::~Server()
{
	Shutdown();
}

// ============================================================================
// PUBLIC INTERFACE
// ============================================================================

bool Server::Initialize()
{
	PrintConfiguration();
	if (!InitializeNetwork()) {
		std::cerr << "Failed to initialize network" << std::endl;
		return false;
	}

	if (!InitializeGame()) {
		std::cerr << "Failed to initialize game" << std::endl;
		return false;
	}

	InitializeMonitor();

	std::cout << "Server ready on port " << m_args.port << std::endl << std::endl;

	return true;
}

int Server::Run()
{
	m_isRunning = true;

	while (m_isRunning) {
		const std::vector<std::unique_ptr<SocketEvent>> events =
			m_socketManager.Poll();

		for (const std::unique_ptr<SocketEvent>& event : events)
			HandleSocketEvent(event);
	}

	return (m_isRunning ? 1 : 0);
}



void Server::Shutdown()
{
	m_isRunning = false;
}

// ============================================================================
// INITIALIZATION
// ============================================================================

bool Server::InitializeNetwork()
{
	if (!m_socketManager.Initialize(m_args))
    {
        std::cerr << "Failed to initialize socketManager" << std::endl;
        return (false);
    }
    return (true);
}


bool Server::InitializeGame()
{
	try {
		m_game = Game::SetInstance(m_args.width, m_args.height);
		if (!m_game) return (false);
		return (true);
	}
	catch (const std::exception& e) {
		std::cerr << "Game exception: " << e.what() << std::endl;
		return (false);
	}
}

void Server::InitializeMonitor()
{
#ifdef _WIN32
	system("start ..\\Monitor\\gfx.exe");
#elif defined(__linux__)
	// system(".\\Monitor\\gfx");
#endif
}

// ============================================================================
// COMMAND HANDLERS
// ============================================================================
//May would in case more events use a map.
void Server::HandleSocketEvent(const std::unique_ptr<SocketEvent>& event)
{
	switch (event->type) {
		case SocketEventType::NewPlayerConnection:
			HandleNewPlayerConnection(event->socket);
			break;
		case SocketEventType::NewAdminConnection:
			HandleNewAdminConnection(event->socket);
			break;
		case SocketEventType::ClientData:
			HandleClientData(event->socket);
			break;
		case SocketEventType::ClientDisconnected:
			HandleClientDisconnection(event->socket);
			break;
		default:
			break;
	}
}

void Server::HandleNewPlayerConnection(ZappySocket *client)
{
	std::cout << "New player connection" << std::endl;

	//Here Goes Player Manager
	client->Send(Messages::Game::Welcome);
}

void Server::HandleNewAdminConnection(ZappySocket *client)
{
	std::cout << "New admin connection" << std::endl;

	// Here Goes Admin
	(void)client;
}

void Server::Send(ClientSocket& client, std::string &str)
{
	(void)client;
	(void)str;
	//	m_socketManager.Send(client.socket, str);
}

void Server::HandleClientDisconnection(ZappySocket *client)
{
	(void)client;
	std::cout << "Client disconnected" << std::endl;
}

void Server::HandleClientData(ZappySocket *client)
{
	ClientSocket *cs = m_socketManager.FindClientByZappySocket(*client);

	if (cs == nullptr)
		return ;
	m_socketManager.ReadAvailableMessages(*cs);
	std::cout << "Readed AvalableMessages" << std::endl;
};

void Server::HandleCommand(const std::string& cmd, Connection* client)
{
	if (cmd.empty()) {
		client->SendLine("ko");
		return;
	}

	// Game commands
	if (cmd == "inventaire") {
		client->SendLine("{nourriture 12, linemate 1, deraumere 0, sibur 2, mendiane 0, phiras 1, thystame 0}");
	}
	else if (cmd == "voir") {
		client->SendLine("{nourriture linemate, sibur, phiras phiras,}");
	}
	else if (cmd == "avance" || cmd == "droite" || cmd == "gauche") {
		client->SendLine("ok");
	}
	else if (cmd.rfind("prend ", 0) == 0) {
		client->SendLine("ok");
	}
	else if (cmd.rfind("pose ", 0) == 0) {
		client->SendLine("ok");
	}
	else if (cmd == "expulse") {
		client->SendLine("ok");
	}
	else if (cmd.rfind("broadcast ", 0) == 0) {
		client->SendLine("ok");
	}
	else if (cmd == "incantation") {
		client->SendLine("elevation en cours");
	}
	else if (cmd == "fork") {
		client->SendLine("ok");
	}
	else if (cmd == "connect_nbr") {
		client->SendLine("10");
	}
	// Monitor commands
	else if (cmd == "msz") {
		Map* map = m_game->WorldMap;
		std::ostringstream ss;
		ss << "msz " << map->Width << " " << map->Height;
		client->SendLine(ss.str());
	}
	else if (cmd == "mct") {
		// TODO: Implement full map contents
		client->SendLine("mct");
	}
	else if (cmd == "sgt") {
		std::ostringstream ss;
		ss << "sgt " << m_args.time;
		client->SendLine(ss.str());
	}
	else if (cmd == "tna") {
		// TODO: Send all team names
		client->SendLine("tna");
	}
}

// ============================================================================
// UTILITY
// ============================================================================

void Server::PrintConfiguration() const
{
	std::cout << std::endl << "========================================" << std::endl
		<< "  ZAPPY SERVER" << std::endl
		<< "========================================" << std::endl
		<< "  Port:      " << m_args.port << std::endl
        << "  AdminPort:      " << SocketManager::GetAdminPortNumber(m_args.port) << std::endl
		<< "  Map:       " << m_args.width << "x" << m_args.height << std::endl
		<< "  Time:      " << m_args.time << std::endl
		<< "  MaxClients: " << m_args.clients << std::endl
		<< "  Teams:     ";
	for (size_t i = 0; i < m_args.teams.size(); ++i) {
		if (i > 0) std::cout << ", ";
		std::cout << m_args.teams[i];
	}
	std::cout << std::endl << "========================================" << std::endl << std::endl;
}
