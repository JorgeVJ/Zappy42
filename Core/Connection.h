#pragma once
#define _WINSOCK_DEPRECATED_NO_WARNINGS

#include <winsock2.h>
#include <ws2tcpip.h>
#include <string>

class Connection
{
    public:
        Connection();
        explicit Connection(SOCKET s);
        ~Connection();
        Connection(const Connection&) = delete;
        Connection(Connection&& other) noexcept;

        Connection& operator=(const Connection&) = delete;
        Connection& operator=(Connection&& other) noexcept;
    
        bool IsValid() const;
        SOCKET Get() const;
        bool SendLine(const std::string& line);
        bool RecvLine(std::string& outLine);

    private:
        SOCKET sock = INVALID_SOCKET;
};
