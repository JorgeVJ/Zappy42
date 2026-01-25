#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <string>
#include <vector>
#include <thread>
#include <chrono>

#pragma comment(lib, "Ws2_32.lib")

#include "Blackboard.h"
#include "IAgent.h"
#include "AgentExplorer.h"
#include "AgentHungry.h"
#include "AgentChaman.h"
#include "AgentBreeder.h"
#include "AgentStoner.h"

static bool SendLine(SOCKET sock, const std::string& line)
{
    std::string msg = line;
    if (msg.empty() || msg.back() != '\n')
        msg += "\n";

    int sent = send(sock, msg.c_str(), (int)msg.size(), 0);
    return sent != SOCKET_ERROR;
}

static bool RecvLine(SOCKET sock, std::string& outLine)
{
    outLine.clear();
    char ch;

    while (true)
    {
        int r = recv(sock, &ch, 1, 0);
        if (r <= 0)
            return false;

        if (ch == '\n')
            break;

        if (ch != '\r')
            outLine.push_back(ch);
    }
    return true;
}

Bid* GetBestBid(std::vector<Bid>& bids)
{
    if (bids.empty())
        return nullptr;

    Bid* best = &bids[0];
    for (auto& bid : bids)
        if (bid.Value > best->Value)
            best = &bid;

    return best;
}

void CreateAgents(std::vector<IAgent*>& agents)
{
    agents.push_back(new AgentExplorer());
    agents.push_back(new AgentHungry());
    agents.push_back(new AgentChaman());
    agents.push_back(new AgentBreeder());
    agents.push_back(new AgentStoner());
}

int main()
{
    // -----------------------------
    // 1) Socket connect
    // -----------------------------
    WSADATA wsaData{};
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
    {
        std::cerr << "WSAStartup failed\n";
        return 1;
    }

    SOCKET sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (sock == INVALID_SOCKET)
    {
        std::cerr << "socket() failed\n";
        WSACleanup();
        return 1;
    }

    sockaddr_in serverAddr{};
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(12345);
    inet_pton(AF_INET, "127.0.0.1", &serverAddr.sin_addr);

    if (connect(sock, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
    {
        std::cerr << "connect() failed\n";
        closesocket(sock);
        WSACleanup();
        return 1;
    }

    // -----------------------------
    // 2) Read WELCOME
    // -----------------------------
    std::string line;
    if (!RecvLine(sock, line))
    {
        std::cerr << "Server closed before WELCOME\n";
        closesocket(sock);
        WSACleanup();
        return 1;
    }

    std::cout << "[Server] " << line << "\n";

    // -----------------------------
    // 3) Setup Blackboard + Agents
    // -----------------------------
    Blackboard board(5, 5);
    std::vector<IAgent*> agents;
    CreateAgents(agents);

    // En Zappy real, tras WELCOME suele venir team name, etc.
    // Para mock server, podemos empezar ya.

    while (true)
    {
        // -----------------------------
        // 4) IA decide best bid
        // -----------------------------
        board.Bids.clear();

        for (auto& agent : agents)
            agent->GetBids(board);

        Bid* bestBid = GetBestBid(board.Bids);
        if (!bestBid)
        {
            std::cout << "No hay bids disponibles\n";
            continue;
        }

        // -----------------------------
        // 5) Send command
        // -----------------------------
        std::cout << "[Client] CMD => " << bestBid->Command
            << " (score=" << bestBid->Value << ")\n";

        if (!SendLine(sock, bestBid->Command))
        {
            std::cerr << "send() failed\n";
            break;
        }

        // -----------------------------
        // 6) Wait response
        // -----------------------------
        std::string response;
        if (!RecvLine(sock, response))
        {
            std::cerr << "Server disconnected\n";
            break;
        }

        std::cout << "[Server] RESP <= " << response << "\n";

        // -----------------------------
        // 7) Apply response to Blackboard
        // -----------------------------
        if (bestBid->Command == "inventory" && !response.empty() && response[0] == '{')
        {
            board.Inventory.SetFromServerString(response);
        }
        else if (bestBid->Command == "voir" && !response.empty() && response[0] == '{')
        {
            board.HandleVoirResponse(response);
        }
        else
        {
            // Aquí puedes gestionar ok/ko, mensajes broadcast, etc.
            // board.Messages.push_back(response);
        }

        std::this_thread::sleep_for(std::chrono::seconds(10));
    }

    // -----------------------------
    // Cleanup
    // -----------------------------
    closesocket(sock);
    WSACleanup();

    for (auto* a : agents)
        delete a;

    return 0;
}
