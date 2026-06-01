#include "pch.h"
#include "ZappySocket.h"

ZappySocket::ZappySocket() : sock(INVALID_SOCKET) { }

ZappySocket::ZappySocket(SOCKET s) : sock(s) { }

ZappySocket::ZappySocket(ZappySocket &s) noexcept: sock(s.sock) { }

bool ZappySocket::isValid() const noexcept
{
	return (sock != INVALID_SOCKET);
}

ZappySocket::~ZappySocket()
{
	if (sock != INVALID_SOCKET)
#ifdef _WIN32
      closesocket(sock);
#elif defined(__linux__)
      close(sock);
#endif
}


ZappySocket& ZappySocket::operator=(SOCKET other) noexcept
{
    if (this->sock != other && this->sock != INVALID_SOCKET)
#ifdef _WIN32
          closesocket(this->sock);
#elif defined(__linux__)
          close(this->sock);
#endif

	  this->sock = other;
	  return (*this);
}

ZappySocket& ZappySocket::operator=(ZappySocket& other) noexcept
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

SOCKET ZappySocket::Get() const noexcept
{
	return (sock);
}
