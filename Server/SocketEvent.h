#pragma once
#include "ZappySocket.h"

enum class SocketEventType {
    None,
    NewPlayerConnection,
    NewAdminConnection,
    ClientData,
    ClientDisconnected
};

struct SocketEvent {
    SocketEventType type;
    ZappySocket *socket;
};
