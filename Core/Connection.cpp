#include "pch.h"
#include "Connection.h"

Connection::Connection() = default;

Connection::Connection(SOCKET s)
    : sock(s)
{
}

Connection::~Connection()
{
    if (sock != INVALID_SOCKET)
        closesocket(sock);
}

Connection::Connection(Connection&& other) noexcept
{
    sock = other.sock;
    other.sock = INVALID_SOCKET;
}

Connection& Connection::operator=(Connection&& other) noexcept
{
    if (this != &other)
    {
        if (sock != INVALID_SOCKET)
            closesocket(sock);

        sock = other.sock;
        other.sock = INVALID_SOCKET;
    }
    return *this;
}

bool Connection::IsPlayer() const
{
    return Player != nullptr;
}

bool Connection::IsMonitor() const
{
    return Player == nullptr;
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
