#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>
#include <sstream>
#include <vector>

#pragma comment(lib, "Ws2_32.lib")

static void SendLine(SOCKET client, const std::string& s)
{
    std::string msg = s;
    if (msg.empty() || msg.back() != '\n')
        msg += "\n";

    send(client, msg.c_str(), (int)msg.size(), 0);
}

static std::string Trim(const std::string& str)
{
    size_t start = str.find_first_not_of(" \t\r\n");
    size_t end = str.find_last_not_of(" \t\r\n");
    if (start == std::string::npos) return "";
    return str.substr(start, end - start + 1);
}

static std::string HandleCommand(const std::string& cmd)
{
    // Aquí simulas el server Zappy.
    // Puedes conectarlo luego con tu Map/Tile reales.

    if (cmd == "inventory")
    {
        // Respuesta estilo Zappy
        return "{nourriture 12, linemate 1, deraumere 0, sibur 2, mendiane 0, phiras 1, thystame 0}";
    }
    else if (cmd == "voir")
    {
        // Ejemplo MUY simplificado:
        // En el real, son tiles en forma de cono según nivel/orientación.
        return "{nourriture linemate, sibur, phiras phiras,}";
    }
    else if (cmd == "avance")
    {
        return "ok";
    }
    else if (cmd == "droite")
    {
        return "ok";
    }
    else if (cmd == "gauche")
    {
        return "ok";
    }
    else if (cmd.rfind("prend ", 0) == 0)
    {
        // prend nourriture / prend linemate ...
        return "ok";
    }
    else if (cmd.rfind("broadcast ", 0) == 0)
    {
        return "ok";
    }
    else if (cmd == "fork")
    {
        return "ok";
    }
    else if (cmd == "incantation")
    {
        // En el server real, esto depende de condiciones del tile
        return "elevation en cours";
    }

    return "ko";
}

static void handle_client(SOCKET clientSocket)
{
    std::cout << "[Server] Client connected!\n";

    // Mensaje inicial típico de Zappy
    SendLine(clientSocket, "WELCOME");

    char buffer[1024];
    std::string partial;

    while (true)
    {
        int bytes = recv(clientSocket, buffer, sizeof(buffer) - 1, 0);
        if (bytes <= 0)
        {
            std::cout << "[Server] Client disconnected.\n";
            break;
        }

        buffer[bytes] = '\0';
        partial += buffer;

        // Procesar líneas completas (\n)
        size_t pos;
        while ((pos = partial.find('\n')) != std::string::npos)
        {
            std::string line = partial.substr(0, pos);
            partial.erase(0, pos + 1);

            line = Trim(line);
            if (line.empty())
                continue;

            std::cout << "[Server] Received: " << line << "\n";

            std::string response = HandleCommand(line);
            std::cout << "[Server] Responding: " << response << "\n";

            SendLine(clientSocket, response);
        }
    }

    closesocket(clientSocket);
}

int main() {
    WSADATA wsaData;
    WSAStartup(MAKEWORD(2, 2), &wsaData);

    SOCKET listenSocket = socket(AF_INET, SOCK_STREAM, 0);

    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = INADDR_ANY;
    addr.sin_port = htons(12345);

    bind(listenSocket, (sockaddr*)&addr, sizeof(addr));
    listen(listenSocket, SOMAXCONN);

    std::vector<SOCKET> clients;

    std::cout << "Server listening...\n";

    while (true) {
        fd_set readSet;
        FD_ZERO(&readSet);
        FD_SET(listenSocket, &readSet);

        SOCKET maxSock = listenSocket;

        for (SOCKET s : clients) {
            FD_SET(s, &readSet);
            if (s > maxSock) maxSock = s;
        }

        // Timeout opcional
        timeval timeout{};
        timeout.tv_sec = 1;
        timeout.tv_usec = 0;

        int n = select(int(maxSock + 1), &readSet, nullptr, nullptr, &timeout);
        if (n == SOCKET_ERROR) {
            std::cerr << "select() error\n";
            break;
        }

        // Nueva conexión
        if (FD_ISSET(listenSocket, &readSet)) {
            SOCKET client = accept(listenSocket, nullptr, nullptr);
            if (client != INVALID_SOCKET) {
                std::cout << "Client connected!\n";
                clients.push_back(client);
                send(client, "WELCOME\n", 8, 0);
            }
        }

        // Revisar clientes
        for (size_t i = 0; i < clients.size(); ) {
            SOCKET s = clients[i];
            if (FD_ISSET(s, &readSet)) {
                char buffer[512];
                int ret = recv(s, buffer, sizeof(buffer) - 1, 0);
                if (ret <= 0) {
                    std::cout << "Client disconnected\n";
                    closesocket(s);
                    clients.erase(clients.begin() + i);
                    continue; // no aumentar i
                }
                else {
                    buffer[ret] = '\0';
                    std::cout << "Received: " << buffer;
                    // Aquí procesas la línea y envías respuesta
                }
            }
            i++;
        }
    }

    closesocket(listenSocket);
    WSACleanup();
    return 0;
}

