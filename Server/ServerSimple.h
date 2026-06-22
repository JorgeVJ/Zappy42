#pragma once

#include <iostream>
#include <vector>
#include <string>
#include <memory>
#include "Connection.h"
#include "Game.h"
#include "serveroptions.h"
#include "TeamManager.h"
#include "SocketManager.h"

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

private:
	// ========================================================================
	// INITIALIZATION
	// ========================================================================

	bool InitializeNetwork();
	bool InitializeGame();
	void InitializeMonitor();

	// ========================================================================
	// COMMAND HANDLERS
	// ========================================================================

	void HandleCommand(const std::string& cmd, Connection* client);
	int HandlePlayerConnection(Connection* client, const std::string& teamName);

	// ========================================================================
	// UTILITY
	// ========================================================================

	void PrintConfiguration() const;

	void HandleSocketEvent(const std::unique_ptr<SocketEvent>& event);
	void Send(ClientSocket& client, std::string &str);
	void HandleNewAdminConnection(ZappySocket* client);
	void HandleNewPlayerConnection(ZappySocket* client);
	void HandleClientData(ZappySocket* client);
	void HandleClientDisconnection(ZappySocket* client);

	// ========================================================================
	// MEMBERS
	// ========================================================================

	// Configuration
	Opt::Server::Args m_args;

	// Network
	SocketManager m_socketManager;

	// Game
	Game* m_game = nullptr;

	// State
	bool m_isRunning = false;

};
