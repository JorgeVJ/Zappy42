#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>
#include <sstream>

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

int main()
{
    WSADATA wsaData;
    SOCKET listenSocket = INVALID_SOCKET;

    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
    {
        std::cerr << "WSAStartup failed\n";
        return 1;
    }

    listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (listenSocket == INVALID_SOCKET)
    {
        std::cerr << "socket() failed\n";
        WSACleanup();
        return 1;
    }

    sockaddr_in serverAddr{};
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = INADDR_ANY;
    serverAddr.sin_port = htons(12345);

    if (bind(listenSocket, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
    {
        std::cerr << "bind() failed\n";
        closesocket(listenSocket);
        WSACleanup();
        return 1;
    }

    if (listen(listenSocket, 1) == SOCKET_ERROR)
    {
        std::cerr << "listen() failed\n";
        closesocket(listenSocket);
        WSACleanup();
        return 1;
    }

    std::cout << "[Server] Listening on port 12345...\n";

    SOCKET clientSocket = accept(listenSocket, nullptr, nullptr);
    if (clientSocket == INVALID_SOCKET)
    {
        std::cerr << "accept() failed\n";
        closesocket(listenSocket);
        WSACleanup();
        return 1;
    }

    handle_client(clientSocket);

    closesocket(listenSocket);
    WSACleanup();
    return 0;
}
