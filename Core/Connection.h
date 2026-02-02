#pragma once
#ifdef _WIN32
# define _WINSOCK_DEPRECATED_NO_WARNINGS
# include <winsock2.h>
# include <ws2tcpip.h>
#elif defined(__linux__)
# include <netinet/in.h>
# include <sys/socket.h>
# define INVALID_SOCKET -1
# define SOCKET_ERROR -1
#else
#error "Unexpected OS"
#endif
#include <string>
#include "Point.h"
#include "Player.h"
#include "Direction.h"

class Connection
{
public:
	Player* player;

	Connection();
#ifdef  _WIN32
	explicit Connection(SOCKET s);
#elif  defined(__linux__)
	explicit Connection(int s);
#else
#error "Unexpected OS"
#endif

	~Connection();
	Connection(const Connection&) = delete;
	Connection(Connection&& other) noexcept;

	Connection& operator=(const Connection&) = delete;
	Connection& operator=(Connection&& other) noexcept;

	bool IsPlayer() const;
	bool IsMonitor() const;
	bool IsValid() const;
#ifdef  _WIN32
	SOCKET Get() const;
#elif  defined(__linux__)
	int Get() const;
#else
#error "Unexpected OS"
#endif
	bool SendLine(const std::string& line);
	bool RecvLine(std::string& outLine);

private:
#ifdef _WIN32
    SOCKET sock;
#elif defined(__linux__)
    int sock;
#else
#error "Unexpected OS"
#endif
};
