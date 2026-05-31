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

class ZappySocket
{
  public:
	ZappySocket();
	ZappySocket(SOCKET s);
	ZappySocket(const ZappySocket&);
	~ZappySocket();
	bool isValid() const noexcept;
	ZappySocket(ZappySocket& other) noexcept;

	ZappySocket& operator=(const ZappySocket&) = default;
	ZappySocket& operator=(ZappySocket&& other) noexcept;
	ZappySocket& operator=(SOCKET other) noexcept;
	SOCKET sock;
};
