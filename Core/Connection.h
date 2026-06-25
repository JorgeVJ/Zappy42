#pragma once
#include <string>
#include "Point.h"
#include "Player.h"
#include "Direction.h"
#include "ZappySocket.h"

class Connection
{
  public:
	Player* player;

	Connection();
	explicit Connection(SOCKET s);
	Connection(ZappySocket &s);

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
    ZappySocket sock;
};
