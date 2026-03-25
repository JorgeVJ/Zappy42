#include "pch.h"
#include "ServerSimple.h"
#include "responses.h"
#include "events.h"
#include <algorithm>
#include <sstream>

#ifdef _WIN32
#pragma comment(lib, "Ws2_32.lib")
#endif

// ============================================================================
// CONSTRUCTOR & DESTRUCTOR
// ============================================================================

Server::Server(const Opt::Server::Args& args)
	: m_args(args), m_listenSocket(INVALID_SOCKET), m_game(nullptr), m_isRunning(false)
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

#ifdef _WIN32
	WSADATA wsaData;
	if (WSAStartup(MAKEWORD(2, 2), &wsaData)) {
		std::cerr << "WSAStartup failed" << std::endl;
		return false;
	}
#endif

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

	while (m_isRunning)
	{
		fd_set readSet;
		FD_ZERO(&readSet);

		// Add listen socket
		FD_SET(m_listenSocket.Get(), &readSet);
		SOCKET maxSock = m_listenSocket.Get();

		// Add all client sockets
		for (auto& c : m_clients) {
			FD_SET(c->Get(), &readSet);
			if (c->Get() > maxSock)
				maxSock = c->Get();
		}

		// Set timeout
		timeval timeout{};
		timeout.tv_sec = SELECT_TIMEOUT_SEC;

		// Wait for activity
		int n = select(int(maxSock + 1), &readSet, nullptr, nullptr, &timeout);
		if (n == SOCKET_ERROR) {
			std::cerr << "select() error" << std::endl;
			break;
		}
		// Handle new connections
		if (FD_ISSET(m_listenSocket.Get(), &readSet)) {
			AcceptNewClient(); //
		}
    else { // Handle old connection.
				ProcessClientInput();
    }
	}

#ifdef _WIN32
	WSACleanup();
#endif

	return m_isRunning ? 1 : 0;
}

void Server::Shutdown()
{
	m_isRunning = false;
	CleanupClients();
}

// ============================================================================
// INITIALIZATION
// ============================================================================

bool Server::InitializeNetwork()
{
	SOCKET rawListen = socket(AF_INET, SOCK_STREAM, 0);
	if (rawListen == INVALID_SOCKET) {
		return false;
	}

	m_listenSocket = Connection(rawListen);

	// Set socket options
#ifdef _WIN32
	char reuse = 1;
	setsockopt(m_listenSocket.Get(), SOL_SOCKET, SO_REUSEADDR, &reuse, sizeof(reuse));
#else
	int reuse = 1;
	setsockopt(m_listenSocket.Get(), SOL_SOCKET, SO_REUSEADDR, &reuse, sizeof(reuse));
#endif

	// Bind
	sockaddr_in addr{};
	addr.sin_family = AF_INET;
	addr.sin_addr.s_addr = INADDR_ANY;
	addr.sin_port = htons(m_args.port);

	if (bind(m_listenSocket.Get(), (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
		return false;
	}

	// Listen
	if (listen(m_listenSocket.Get(), SOCKET_BACKLOG) == SOCKET_ERROR) {
		return false;
	}

	return true;
}

bool Server::InitializeGame()
{
	try {
		m_game = Game::SetInstance(m_args.width, m_args.height);
		if (!m_game) return false;
		
		// TODO: Configure game with args
		// m_game->Configure(m_args.width, m_args.height, m_args.time);
		
		return true;
	}
	catch (const std::exception& e) {
		std::cerr << "Game exception: " << e.what() << std::endl;
		return false;
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
// NETWORK OPERATIONS
// ============================================================================

bool Server::AcceptNewClient()
{
	SOCKET s = accept(m_listenSocket.Get(), nullptr, nullptr);
	if (s == INVALID_SOCKET) {
		return (false);
	}

	try {
		Connection* client = new Connection(s);
		if (!client) return (false);

		if (!client->SendLine("BIENVENUE")) {
			delete client;
			return (false);
		}
    std::string cmd;
    if (!client->RecvLine(cmd))
      {
      	delete client;
        return (false);
      }
    // Todo Get a better way to detect type of connection.
    if (cmd == "GRAPHIC") {
      // Register as monitor
      if (client->player) {
        delete client->player;
        client->player = nullptr;
      }
      m_game->Monitors.push_back(client);
      auto it = std::find(m_game->Players.begin(), m_game->Players.end(), client);
      if (it != m_game->Players.end()) {
        m_game->Players.erase(it);
      }
    }

    else {
      // Assume team name - try to register as player
      Game* game = Game::GetInstance();
      auto itp = std::find(game->Players.begin(), game->Players.end(), client);
      auto itm = std::find(game->Monitors.begin(), game->Monitors.end(), client);

      if (itp == game->Players.end() && itm == game->Monitors.end()) {
        HandlePlayerConnection(client, cmd);
      }
      else {
        client->SendLine("ko");
      }
    }
		m_clients.push_back(client);
		std::cout << "Client connected [" << m_clients.size() << "]" << std::endl;
		return (true);
	}
	catch (...) {
		return (false);
	}
}

void Server::ProcessClientInput()
{
	for (size_t i = 0; i < m_clients.size();) {
		Connection* client = m_clients[i];
		
		std::string msg;
		if (!client->RecvLine(msg)) {
			std::cout << "Client disconnected" << std::endl;
			RemoveClient(i);
			continue;
		}

		if (!msg.empty()) {
			HandleCommand(msg, client);
		}
		++i;
	}
}

void Server::RemoveClient(size_t index)
{
	if (index < m_clients.size()) {
		delete m_clients[index];
		m_clients.erase(m_clients.begin() + index);
	}
}

void Server::CleanupClients()
{
	for (auto& c : m_clients) {
		delete c;
	}
	m_clients.clear();
}

// ============================================================================
// COMMAND HANDLERS
// ============================================================================

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

int Server::HandlePlayerConnection(Connection* client, const std::string& teamName)
{
	try {
		client->player = new Player();
		if (!client->player) return 1;

		client->player->TeamName = teamName;
		m_game->Players.push_back(client);

		// Notify monitors
		for (auto* monitor : m_game->Monitors) {
			pnw(client, monitor);
		}

		client->SendLine("1");
		Map* map = m_game->WorldMap;
		std::ostringstream ss;
		ss << map->Width << " " << map->Height;
		client->SendLine(ss.str());

		return 0;
	}
	catch (...) {
		return 1;
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

SOCKET Server::GetListenSocket() const
{
	return m_listenSocket.Get();
}
