#include "SocketManager.h"

SocketManager::SocketManager() : m_serverPlayerSocket(), m_serverAdminSocket() {}

SocketManager::~SocketManager() {
	for (auto& socket : m_playerSockets)
		delete socket;
	m_playerSockets.clear();
	for (auto& socket : m_adminSockets)
		delete socket;
	m_adminSockets.clear();
}

bool SocketManager::ConfigureSocketForReuse(ZappySocket &socket)
{
#ifdef _WIN32
	char reuse = 1;
	return (setsockopt(socket.Get(), SOL_SOCKET, SO_REUSEADDR, &reuse, sizeof(reuse)));
#else
	int reuse = 1;
	return (setsockopt(socket.Get(), SOL_SOCKET, SO_REUSEADDR, &reuse, sizeof(reuse)));
#endif
}

bool SocketManager::InitializeServerSocket(const int portNumber, ZappySocket *ServerSocket)
{
	if (ServerSocket->Get() == SOCKET_ERROR)
		return (false);
	*ServerSocket = ZappySocket(socket(AF_INET, SOCK_STREAM, 0));
	if (ServerSocket->Get() == INVALID_SOCKET)
		return (false);
	SocketManager::ConfigureSocketForReuse(*ServerSocket);
	sockaddr_in addr{};
	addr.sin_family = AF_INET;
	addr.sin_addr.s_addr = INADDR_ANY;
	addr.sin_port = htons(portNumber);
	return (bind(ServerSocket->Get(), (sockaddr*)&addr, sizeof(addr))
			!= SOCKET_ERROR
			&& listen(ServerSocket->Get(), SOCKET_BACKLOG) != SOCKET_ERROR);
}

bool SocketManager::InitializePlayerSocket(Opt::Server::Args m_args)
{
	return (InitializeServerSocket(htons(m_args.port), &this->m_serverPlayerSocket));
}

int SocketManager::GetAdminPortNumber(const int PortNumber) noexcept {
	if (PortNumber == Validators::Port::Max)
		return (Validators::Port::Max - 1);
	return (PortNumber + 1);
}

bool SocketManager::InitializeAdminSocket(Opt::Server::Args m_args)
{
	return (InitializeServerSocket(htons(GetAdminPortNumber(m_args.port)), &this->m_serverAdminSocket));
}

bool SocketManager::AcceptConnection(ZappySocket  &socket, std::vector<ZappySocket*> &sockets) noexcept {
	if (socket.Get() == INVALID_SOCKET)
		return (false);
	try {
		ZappySocket *Socket = new ZappySocket(accept(m_serverPlayerSocket.Get(), nullptr, nullptr));
		if (Socket == nullptr || Socket->Get() == INVALID_SOCKET)
			return (false);
		sockets.push_back(Socket);
	} catch (const std::exception& e) {
		std::cerr << "Exception STL: " << e.what() << std::endl;
		return (false);
	} catch (...) {
		std::cerr << "Exception No STL" << std::endl;
		return (false);
	}
	return (true);
}

bool SocketManager::AcceptPlayerConnection() noexcept {
    return (AcceptConnection(m_serverPlayerSocket, m_playerSockets));
}

bool SocketManager::AcceptAdminConnection() noexcept {
	return (AcceptConnection(m_serverAdminSocket, m_adminSockets));
}

ZappySocket &SocketManager::GetPlayerServerSocket() noexcept {
    return (this->m_serverPlayerSocket);
}

ZappySocket &SocketManager::GetAdminServerSocket() noexcept {
    return (this->m_serverAdminSocket);
}
const std::vector<ZappySocket*>& SocketManager::GetPlayerSockets() const noexcept {
	return (this->m_playerSockets);
}

const std::vector<ZappySocket*>& SocketManager::GetAdminSockets() const noexcept {
	return (this->m_adminSockets);
}
