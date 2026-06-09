#include "pch.h"
#include "Connection.h"

Connection::Connection() : player(nullptr), sock(INVALID_SOCKET) { }

Connection::Connection(ZappySocket &s) : player(nullptr), sock(s) { }
Connection::Connection(SOCKET s) : player(nullptr), sock(s) { }

Connection::~Connection()
{
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
        this->sock = other.sock;
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
    return (sock.isValid());
}


SOCKET Connection::Get() const
{
    return sock.sock;
}

bool Connection::SendLine(const std::string& line)
{
    std::string data = line + "\n";
    return send(this->Get(), data.c_str(), int(data.size()), 0) > 0;
}

bool Connection::RecvLine(std::string& outLine)
{
    outLine.clear();
    char ch;

    while (true)
    {
        int r = recv(this->Get(), &ch, 1, 0);
        if (r <= 0)
            return false;

        if (ch == '\n')
            break;

        if (ch != '\r')
            outLine.push_back(ch);
    }
    return true;
}
