#include "pch.h"
#include "ZappySocket.h"

ZappySocket::ZappySocket() : socket(INVALID_SOCKET) { }

ZappySocket::ZappySocket(SOCKET s) : socket(s) { }

ZappySocket::ZappySocket(ZappySocket &s) noexcept: socket(s.socket) { }

bool ZappySocket::isValid() const noexcept
{
	return (socket != INVALID_SOCKET);
}

ZappySocket::~ZappySocket()
{
	if (socket != INVALID_SOCKET)
#ifdef _WIN32
      closesocket(socket);
#elif defined(__linux__)
      close(socket);
#endif
}


ZappySocket& ZappySocket::operator=(SOCKET other) noexcept
{
    if (this->socket != other && this->socket != INVALID_SOCKET)
#ifdef _WIN32
          closesocket(this->socket);
#elif defined(__linux__)
          close(this->socket);
#endif

	  this->socket = other;
	  return (*this);
}

ZappySocket& ZappySocket::operator=(ZappySocket& other) noexcept
{
    if (this != &other)
    {
        if (socket != INVALID_SOCKET)
#ifdef _WIN32
          closesocket(socket);
#elif defined(__linux__)
          close(socket);
#endif
        socket = other.socket;
        other.socket = INVALID_SOCKET;
    }
    return *this;
}

int ZappySocket::GetLastError() noexcept
{
#ifdef _WIN32
    return (WSAGetLastError());
#else
    return (errno);
#endif
}

bool ZappySocket::IsWouldBlockError() noexcept
{
    const int error = GetLastError();

#ifdef _WIN32
    return (error == WSAEWOULDBLOCK);
#else
    return (error == EWOULDBLOCK || error == EAGAIN);
#endif
}

bool ZappySocket::Send(std::string &data)
{
	return (send(this->Get(), data.c_str(), static_cast<int>(data.size()), 0) == static_cast<int>(data.size()));
}

bool ZappySocket::Send(const std::string_view &data)
{
	return (send(this->Get(), data.data(), static_cast<int>(data.size()), 0) == static_cast<int>(data.size()));
}

bool ZappySocket::SetNonBlocking(bool enabled) noexcept
{
#ifdef _WIN32

    u_long mode = enabled ? 1 : 0;

    return ioctlsocket(socket, FIONBIO, &mode) == 0;

#else

    int flags = fcntl(socket, F_GETFL, 0);

    if (flags < 0)
	{
		printf("socket fd: %d\n", socket);
		perror("fcntl error");
        return false;
	}
    if (enabled)
        flags |= O_NONBLOCK;
    else
        flags &= ~O_NONBLOCK;

    return fcntl(socket, F_SETFL, flags) == 0;

#endif
}

SOCKET ZappySocket::Get() const noexcept
{
	return (socket);
}
