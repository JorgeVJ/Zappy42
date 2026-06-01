// SocketManager.h
#pragma once

#include <iostream>
#include <vector>
#include <string>
#include <memory>
#include <sys/socket.h>
#include <netinet/in.h>
#include "Connection.h"
#include "validators.h"
#include "serveroptions.h"

class SocketManager {
public:
    SocketManager();
    ~SocketManager();

    // Initialize a new socket for players/monitors
	bool InitializePlayerSocket(Opt::Server::Args m_args);

    // Initialize a new socket for admins
	bool InitializeAdminSocket(Opt::Server::Args m_args);

	static bool ConfigureSocketForReuse(ZappySocket &socket);

    // Get all player/monitor sockets
    const std::vector<ZappySocket*>& GetPlayerSockets() const noexcept;

    // Check if the given socket is the server socket
    bool IsServerSocket(ZappySocket& socket) const noexcept;

    // Get the admin socket
    ZappySocket &GetAdminServerSocket() noexcept;
	ZappySocket &GetPlayerServerSocket() noexcept;


    // Add a new socket to the player/monitor sockets
    bool AcceptPlayerConnection() noexcept;

    // Remove a socket from the player/monitor sockets
    void RemovePlayerSocket(ZappySocket& socket);


	bool AcceptAdminConnection() noexcept;
	void RemoveAdminSocket(ZappySocket& socket);
	const std::vector<ZappySocket*>& GetAdminSockets() const noexcept;
	static int GetAdminPortNumber(const int PortNumber) noexcept;

private:


	bool AcceptConnection(ZappySocket &socket, std::vector<ZappySocket*> &sockets) noexcept;
	static bool InitializeServerSocket(const int portNumber, ZappySocket *socket);
    std::vector<ZappySocket*> m_playerSockets;
	std::vector<ZappySocket*> m_adminSockets;
	ZappySocket m_serverPlayerSocket;
	ZappySocket m_serverAdminSocket;
};
