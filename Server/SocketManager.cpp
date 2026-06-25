 #include "SocketManager.h"

SocketManager::SocketManager() : m_serverPlayerSocket(), m_serverAdminSocket() {}

SocketManager::~SocketManager() {}

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
	if (ServerSocket->Get() != SOCKET_ERROR)
		return (false);
	*ServerSocket = socket(AF_INET, SOCK_STREAM, 0);
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
	return (InitializeServerSocket(m_args.port, &this->m_serverPlayerSocket));
}

int SocketManager::GetAdminPortNumber(const int PortNumber) noexcept {
	if (PortNumber == Validators::Port::Max)
		return (Validators::Port::Max - 1);
	return (PortNumber + 1);
}

bool SocketManager::InitializeAdminSocket(Opt::Server::Args m_args)
{
	return (InitializeServerSocket(GetAdminPortNumber(m_args.port), &this->m_serverAdminSocket));
}

bool SocketManager::Initialize(Opt::Server::Args m_args)
{
	return (InitializeAdminSocket(m_args) && InitializePlayerSocket(m_args));
}

bool SocketManager::AcceptConnection(ZappySocket& serverSocket, ClientType type,
									 std::vector<std::unique_ptr<SocketEvent>>& events) noexcept
{
    SOCKET fd = accept(serverSocket.Get(), nullptr, nullptr);

    if (fd == INVALID_SOCKET)
        return false;

    try
    {
        auto client = std::make_unique<ClientSocket>();
        client->type = type;
        client->socket = fd;
        if (!client->socket.SetNonBlocking(true))
            return false;
        ClientSocket* ptr = client.get();
        m_clients.push_back(std::move(client));
        events.push_back(std::make_unique<SocketEvent>(type == ClientType::Game
													   ? SocketEventType::NewPlayerConnection
													   : SocketEventType::NewAdminConnection,
													   &ptr->socket));
    }
    catch (...) {
        return false;
	}
    return true;
}

bool SocketManager::IsSocketReadable(const ClientSocket& client, fd_set& readfds) const
{
	return (FD_ISSET(client.socket.Get(), &readfds) != 0);
}

bool SocketManager::HasSocketDisconnected(ClientSocket& client)
{
	char peek;

	const int result = recv(client.socket.Get(), &peek, 1, MSG_PEEK);
	if (result == 0)
		return (true);
	return (result < 0 && !ZappySocket::IsWouldBlockError());
}

void SocketManager::HandleReadableClient(std::vector<std::unique_ptr<SocketEvent>>& events, ClientSocket& client)
{
	if (HasSocketDisconnected(client)) {
		client.disconnecting = true;
		events.push_back(std::make_unique<SocketEvent>(SocketEventType::ClientDisconnected, &client.socket));
		return;
	}
	events.push_back(std::make_unique<SocketEvent>(SocketEventType::ClientData, &client.socket));
}

void SocketManager::HandleNewConnections(fd_set& readfds, std::vector<std::unique_ptr<SocketEvent>>& events)
{
	if (FD_ISSET(m_serverPlayerSocket.Get(), &readfds))
		AcceptConnection(m_serverPlayerSocket, ClientType::Game, events);

	if (FD_ISSET(m_serverAdminSocket.Get(), &readfds))
		AcceptConnection(m_serverAdminSocket, ClientType::Admin, events);
}

SOCKET SocketManager::BuildReadSet(fd_set& readfds)
{
	FD_ZERO(&readfds);
	SOCKET maxFd = 0;

	FD_SET(this->m_serverPlayerSocket.Get(), &readfds);
	maxFd = m_serverPlayerSocket.Get();
	FD_SET(this->m_serverAdminSocket.Get(), &readfds);
	if (m_serverAdminSocket.Get() > maxFd)
		maxFd = m_serverAdminSocket.Get();
	for (const auto& client : m_clients) {
		if (client->disconnecting)
			continue;
		const SOCKET fd = client->socket.Get();
		FD_SET(fd, &readfds);
		if (fd > maxFd)
			maxFd = fd;
	}
	return (maxFd);
}

void SocketManager::CleanupDisconnectedClients()
{
	std::erase_if(m_clients,
		[](const auto& client) {
			return (client->disconnecting);
		}
	);
}

std::vector<std::unique_ptr<SocketEvent>> SocketManager::Poll()
{
	std::vector<std::unique_ptr<SocketEvent>> events;
	fd_set readfds;
	const SOCKET maxFd = BuildReadSet(readfds);
	timeval timeout{};

	timeout.tv_sec = SocketManager::SELECT_TIMEOUT_SEC;
	const int activity = select(static_cast<int>(maxFd + 1), &readfds, nullptr, nullptr, &timeout);
	if (activity <= 0)
		return (events);
	HandleNewConnections(readfds, events);
	for (const auto& client : m_clients) {
		if (client->disconnecting)
			continue;
		if (!IsSocketReadable(*client, readfds))
			continue;
		HandleReadableClient(events, *client);
	}
	CleanupDisconnectedClients();
	return (events);
}

MessageReadResult SocketManager::ReadAvailableMessages(ClientSocket& client)
{
    MessageReadResult result;
    if (DrainSocketData(client, result))
      ExtractMessages(client, result);
    return (result);
}

bool SocketManager::DrainSocketData(ClientSocket& client, MessageReadResult& result)
{
    char buffer[SOCKET_READ_BUFFER_SIZE];
	int received;
    while (true)
    {
        received = recv(client.socket.Get(), buffer, SOCKET_READ_BUFFER_SIZE, 0);
        if (received == 0)
        {
            client.disconnecting = true;
            return (false);
        }
        if (received < 0)
        {
            if (ZappySocket::IsWouldBlockError())
                break;
			client.disconnecting = true;
            return (false);
        }
        client.pendingMessage.append(buffer, received);
        if (client.pendingMessage.size() > MAX_PENDING_MESSAGE_SIZE)
        {
            result.overflow = true;
            client.disconnecting = true;
            return (false);
        }
    }
    return (true);
}

ClientSocket* SocketManager::FindClientByZappySocket(const ZappySocket& zs) noexcept {
    const SOCKET target = zs.Get();
    for (auto& c : m_clients) {
        if (c && c->socket.isValid() && c->socket.Get() == target)
            return c.get();
    }
    return nullptr;
}

const ClientSocket* SocketManager::FindClientByZappySocket(const ZappySocket& zs) const noexcept {
    const SOCKET target = zs.Get();
    for (auto& c : m_clients) {
        if (c && c->socket.isValid() && c->socket.Get() == target)
            return c.get();
    }
    return nullptr;
}


void SocketManager::ExtractMessages(ClientSocket& client, MessageReadResult& result)
{
    size_t newlinePos;

    while ((newlinePos = client.pendingMessage.find('\n')) != std::string::npos
			&& result.messages.size() < MAX_MESSAGES_PER_READ)
    {
        result.messages.push_back(client.pendingMessage.substr(0, newlinePos));
        client.pendingMessage.erase(0, newlinePos + 1);
    }
}
size_t SocketManager::GetAmmountMonitors(bool disconnecting) const {
	return CountByType(ClientType::Monitor, disconnecting);
}

size_t SocketManager::GetAmmountPlayers(bool disconnecting) const {
	return CountByType(ClientType::Player, disconnecting);
}

size_t SocketManager::GetAmmountAdmins(bool disconnecting) const {
	return CountByType(ClientType::Admin, disconnecting);
}

size_t SocketManager::CountByType(ClientType wanted, bool disconnecting) const {
	size_t count = 0;
	for (const auto& c : m_clients) {
		if (!c)
			continue;
		if (!disconnecting && c->disconnecting)
			continue;   // optional; remove if you want to count them
		if (c->type == wanted)
			++count;
    }
    return count;
}
