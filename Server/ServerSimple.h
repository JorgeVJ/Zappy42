#pragma once

#include <iostream>
#include <vector>
#include <string>
#include <memory>
#include "Connection.h"
#include "Game.h"
#include "serveroptions.h"
#include "TeamManager.h"

#ifdef _WIN32
#include <winsock2.h>
#else
#include <sys/socket.h>
#include <netinet/in.h>
#endif

/// <summary>
/// Zappy game server - Simple, clean implementation
///
/// Features:
/// - Encapsulates game server logic
/// - Manages client connections
/// - Handles game commands
/// - Result<T> based validation
///
/// Future: SSL can be added separately without changing this core logic
/// </summary>
class Server {
public:
	/// <summary>
	/// Creates server with validated arguments
	/// </summary>
	explicit Server(const Opt::Server::Args& args);

	/// <summary>
	/// Destructor - cleans up resources
	/// </summary>
	~Server();

	// Prevent copying
	Server(const Server&) = delete;
	Server& operator=(const Server&) = delete;

	/// <summary>
	/// Initializes server (network, game)
	/// </summary>
	bool Initialize();

	/// <summary>
	/// Runs main server loop
	/// </summary>
	int Run();

	/// <summary>
	/// Gracefully shuts down server
	/// </summary>
	void Shutdown();

	// Accessors
	const Opt::Server::Args& GetArgs() const { return m_args; }
	Game* GetGame() const { return m_game; }
	size_t GetClientCount() const { return m_clients.size(); }

private:
	// ========================================================================
	// INITIALIZATION
	// ========================================================================

	bool InitializeNetwork();
	bool InitializeGame();
	void InitializeMonitor();

	// ========================================================================
	// NETWORK OPERATIONS
	// ========================================================================

	bool AcceptNewClient();
	void ProcessClientInput();
	void RemoveClient(size_t index);
	void CleanupClients();

	// ========================================================================
	// COMMAND HANDLERS
	// ========================================================================

	void HandleCommand(const std::string& cmd, Connection* client);
	int HandlePlayerConnection(Connection* client, const std::string& teamName);

	// ========================================================================
	// UTILITY
	// ========================================================================

	void PrintConfiguration() const;
	SOCKET GetListenSocket() const;

	// ========================================================================
	// MEMBERS
	// ========================================================================

	// Configuration
	Opt::Server::Args m_args;

	// Network
	Connection m_listenSocket;
	std::vector<Connection*> m_clients;

	// Game
	Game* m_game = nullptr;

	// State
	bool m_isRunning = false;

	// Constants
	static constexpr int SELECT_TIMEOUT_SEC = 1;
	//static constexpr int SOCKET_BACKLOG = SOMAXCONN;
};
