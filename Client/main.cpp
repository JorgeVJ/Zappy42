#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <vector>
#include <thread>
#include <chrono>

#pragma comment(lib, "Ws2_32.lib")

#include "Connection.h"
#include "Blackboard.h"
#include "IAgent.h"
#include "AgentExplorer.h"
#include "AgentHungry.h"
#include "AgentChaman.h"
#include "AgentBreeder.h"
#include "AgentStoner.h"

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
    WSADATA wsaData{};
    WSAStartup(MAKEWORD(2, 2), &wsaData);

    SOCKET rawSock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    Connection sock(rawSock);

    sockaddr_in serverAddr{};
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(12345);
    inet_pton(AF_INET, "127.0.0.1", &serverAddr.sin_addr);

    if (connect(sock.Get(), (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
    {
        std::cerr << "connect() failed\n";
        return 1;
    }

    std::string line;
    if (!sock.RecvLine(line))
    {
        std::cerr << "Server closed before WELCOME\n";
        return 1;
    }

    std::cout << "[Server] " << line << "\n";

    std::string teamName = "TestTeamName";
    sock.SendLine(teamName);

    // TODO: En este punto se deben recibir dos mensajes del servidor
    // uno con la posibilidad de conección y
    // otro con las dimensiones del mapa.
    Blackboard board(5, 5);
    std::vector<IAgent*> agents;
    CreateAgents(agents);

    while (true)
    {
        board.Bids.clear();
        for (auto& agent : agents)
            agent->GetBids(board);

        Bid* bestBid = GetBestBid(board.Bids);
        if (!bestBid)
            continue;

        std::cout << "[Client] CMD => " << bestBid->Command << "\n";

        if (!sock.SendLine(bestBid->Command))
            break;

        std::string response;
        if (!sock.RecvLine(response))
            break;

        std::cout << "[Server] RESP <= " << response << "\n";

        std::this_thread::sleep_for(std::chrono::seconds(10));
    }

    for (auto* a : agents)
        delete a;

    WSACleanup();
    return 0;
}
