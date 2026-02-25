#include "pch.h"
#include "Connection.h"

Connection::Connection() : player(nullptr), sock(INVALID_SOCKET) { }

Connection::Connection(SOCKET s) : player(nullptr), sock(s) { }

Connection::~Connection()
{
    if (sock != INVALID_SOCKET)
#ifdef _WIN32
      closesocket(sock);
#elif defined(__linux__)
      close(sock);
#endif

    if (player)
    {
        delete player;
        player = nullptr;
    }
}

Connection::Connection(Connection&& other) noexcept
{
    player = nullptr;
    sock = other.sock;
    other.sock = INVALID_SOCKET;
}

Connection& Connection::operator=(Connection&& other) noexcept
{
    if (this != &other)
    {
        if (sock != INVALID_SOCKET)
#ifdef _WIN32
          closesocket(sock);
#elif defined(__linux__)
          close(sock);
#endif
        sock = other.sock;
        other.sock = INVALID_SOCKET;
    }
    return *this;
}

bool Connection::IsPlayer() const
{
    return player != nullptr;
}

bool Connection::IsMonitor() const
{
    return player == nullptr;
}

bool Connection::IsValid() const
{
    return sock != INVALID_SOCKET;
}


SOCKET Connection::Get() const
{
    return sock;
}

bool Connection::SendLine(const std::string& line)
{
    std::string data = line + "\n";
    return send(sock, data.c_str(), int(data.size()), 0) > 0;
}

bool Connection::RecvLine(std::string& outLine)
{
    outLine.clear();
    char ch;

    while (true)
    {
        int r = recv(sock, &ch, 1, 0);
        if (r <= 0)
            return false;

        if (ch == '\n')
            break;

        if (ch != '\r')
            outLine.push_back(ch);
    }
    return true;
}
