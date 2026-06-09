// SocketManager.h
#pragma once

#include <iostream>
#include <vector>
#include <unordered_map>
#include <string>
#include <memory>
#include "Connection.h"
#include "SocketEvent.h"
#include "validators.h"
#include "serveroptions.h"

enum class ClientType {
    Player,
    Admin
};

struct ClientSocket {
    ClientType type;

    ZappySocket socket;

    std::string pendingMessage;
	bool disconnecting = false;
};

struct MessageReadResult {
	bool overflow = false;
	std::vector<std::string> messages;
};

class SocketManager {
  public:
    SocketManager();
    ~SocketManager();

	bool Initialize(Opt::Server::Args m_args);
	std::vector<std::unique_ptr<SocketEvent>> Poll();
	MessageReadResult ReadAvailableMessages(ClientSocket& client);
	static bool ConfigureSocketForReuse(ZappySocket &socket);
	static int GetAdminPortNumber(const int PortNumber) noexcept;
	static bool InitializeServerSocket(const int portNumber, ZappySocket *socket);
  private:
    // Initialize a new socket for players/monitors
	bool InitializePlayerSocket(Opt::Server::Args m_args);
    // Initialize a new socket for admins
	bool InitializeAdminSocket(Opt::Server::Args m_args);
	bool AcceptConnection(ZappySocket& serverSocket, ClientType type,
						  std::vector<std::unique_ptr<SocketEvent>>& events) noexcept;

  private:
	void HandleReadableClient(std::vector<std::unique_ptr<SocketEvent>>& events, ClientSocket& client);
	void HandleNewConnections(fd_set& readfds, std::vector<std::unique_ptr<SocketEvent>>& events);
	SOCKET BuildReadSet(fd_set& readfds);
	bool IsSocketReadable(const ClientSocket& client, fd_set& readfds) const;
	bool HasSocketDisconnected(ClientSocket& client);
	void CleanupDisconnectedClients();
	bool DrainSocketData(ClientSocket& client, MessageReadResult& result);
	void ExtractMessages(ClientSocket& client, MessageReadResult& result);
	static constexpr size_t SOCKET_READ_BUFFER_SIZE = 4096;
	static constexpr size_t MAX_PENDING_MESSAGE_SIZE = 4096;
	static constexpr size_t MAX_MESSAGES_PER_READ = 10;
	static constexpr int SELECT_TIMEOUT_SEC = 1;
	ZappySocket m_serverPlayerSocket;
	ZappySocket m_serverAdminSocket;
  	std::vector<std::unique_ptr<ClientSocket>> m_clients;
	std::unordered_map<ZappySocket*, std::string> m_partialMessages;
};
