#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>
#include <sstream>
#include <vector>
#include "Game.h"
#include "responses.h"
#include <utils.h>
#include "Connection.h"
#include <cstdlib>

#pragma comment(lib, "Ws2_32.lib")

void HandleCommand(const std::string& cmd, Connection& client)
{
    // Aquí simulas el server Zappy.
    // Puedes conectarlo luego con tu Map/Tile reales.

    if (cmd == "inventory")
    {
        // Respuesta estilo Zappy
        client.SendLine("{nourriture 12, linemate 1, deraumere 0, sibur 2, mendiane 0, phiras 1, thystame 0}");
    }
    else if (cmd == "voir")
    {
        // Ejemplo MUY simplificado:
        // En el real, son tiles en forma de cono según nivel/orientación.
        client.SendLine("{nourriture linemate, sibur, phiras phiras,}");
    }
    else if (cmd == "avance")
    {
        client.SendLine("ok");
    }
    else if (cmd == "droite")
    {
        client.SendLine("ok");
    }
    else if (cmd == "gauche")
    {
        client.SendLine("ok");
    }
    else if (cmd.rfind("prend ", 0) == 0)
    {
        // prend nourriture / prend linemate ...
        client.SendLine("ok");
    }
    else if (cmd.rfind("broadcast ", 0) == 0)
    {
        client.SendLine("ok");
    }
    else if (cmd == "fork")
    {
        client.SendLine("ok");
    }
    else if (cmd == "incantation")
    {
        // En el server real, esto depende de condiciones del tile
        client.SendLine("elevation en cours");
    }
    else if (cmd == "msz")
    {
        Map* map = Game::GetInstance()->WorldMap;
        std::ostringstream ss;
        ss << "msz " << map->Width << " " << map->Height;
        client.SendLine(ss.str());
    }
    else if (cmd == "mct") {
        std::string fullMap = GetMCT();
        send(client.Get(), fullMap.c_str(), fullMap.size(), 0);
    }
    else
    {
        client.SendLine("ko");
    }
}

int main()
{
    WSADATA wsaData;
    WSAStartup(MAKEWORD(2, 2), &wsaData);

    SOCKET rawListen = socket(AF_INET, SOCK_STREAM, 0);
    Connection listenSocket(rawListen);

    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = INADDR_ANY;
    addr.sin_port = htons(12345);

    bind(listenSocket.Get(), (sockaddr*)&addr, sizeof(addr));
    listen(listenSocket.Get(), SOMAXCONN);

    std::vector<Connection> clients;

    std::cout << "Server listening...\n";
    Game::GetInstance();
    system("start ..\\Monitor\\gfx.exe");

    while (true)
    {
        fd_set readSet;
        FD_ZERO(&readSet);
        FD_SET(listenSocket.Get(), &readSet);

        SOCKET maxSock = listenSocket.Get();

        for (auto& c : clients)
        {
            FD_SET(c.Get(), &readSet);
            if (c.Get() > maxSock)
                maxSock = c.Get();
        }

        timeval timeout{};
        timeout.tv_sec = 1;

        int n = select(int(maxSock + 1), &readSet, nullptr, nullptr, &timeout);
        if (n == SOCKET_ERROR)
        {
            std::cerr << "select() error\n";
            break;
        }

        // Nueva conexión
        if (FD_ISSET(listenSocket.Get(), &readSet))
        {
            SOCKET s = accept(listenSocket.Get(), nullptr, nullptr);
            if (s != INVALID_SOCKET)
            {
                std::cout << "Client connected!\n";
                Connection client(s);
                client.SendLine("WELCOME");
                clients.push_back(std::move(client));
            }
        }

        // Clientes existentes
        for (size_t i = 0; i < clients.size();)
        {
            auto& c = clients[i];
            if (FD_ISSET(c.Get(), &readSet))
            {
                std::string msg;
                if (!c.RecvLine(msg))
                {
                    std::cout << "Client disconnected\n";
                    clients.erase(clients.begin() + i);
                    continue;
                }

                std::cout << "Received: " << msg << "\n";
                HandleCommand(msg.c_str(), c);
            }
            ++i;
        }
    }

    WSACleanup();
    return 0;
}

