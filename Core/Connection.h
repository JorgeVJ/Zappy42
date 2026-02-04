#pragma once
#ifdef _WIN32
# define _WINSOCK_DEPRECATED_NO_WARNINGS
# include <winsock2.h>
# include <ws2tcpip.h>
#elif defined(__linux__)
# include <netinet/in.h>
# include <sys/socket.h>
# include <unistd.h>
# include <arpa/inet.h>
# define INVALID_SOCKET -1
# define SOCKET_ERROR -1
# define SOCKET int
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
	explicit Connection(SOCKET s);

	~Connection();
	Connection(const Connection&) = delete;
	Connection(Connection&& other) noexcept;

	Connection& operator=(const Connection&) = delete;
	Connection& operator=(Connection&& other) noexcept;

	bool IsPlayer() const;
	bool IsMonitor() const;
	bool IsValid() const;
	SOCKET Get() const;
	bool SendLine(const std::string& line);
	bool RecvLine(std::string& outLine);

  private:
    int sock;
};
