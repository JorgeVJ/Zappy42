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
#include "CommandHistory.h"

void WaitForDebugAndClean(int seconds = 5)
{
    std::cout << "\n[DEBUG] Waiting " << seconds << " seconds before closing..." << std::endl;
    std::this_thread::sleep_for(std::chrono::seconds(seconds));
    WSACleanup();
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

int InitServerHandshake(Blackboard& board, const std::string& teamName)
{
    std::string line;
    
    if (!board.Sock->RecvLine(line))
    {
        std::cerr << "Server closed before BIENVENUE\n";
        return 1;
    }
    if (line != "BIENVENUE")
    {
        std::cerr << "Server message error: expected 'BIENVENUE', got '" << line << "'\n";
        return 1;
    }
    
    std::cout << "[Server] " << line << std::endl;
    
    if (!board.Sock->SendLine(teamName))
    {
        std::cerr << "Failed to send team name\n";
        return 1;
    }

    if (!board.Sock->RecvLine(line))
    {
        std::cerr << "Failed to receive nb-client\n";
        return 1;
    }
    
    int nb_client = 0;
    try
    {
        size_t pos = 0;
        nb_client = std::stoi(line, &pos);
        
        // Verificar que toda la linea fue parseada (no hay caracteres extra)
        if (pos != line.length())
        {
            std::cerr << "Invalid nb-client format: '" << line << "' (extra characters)\n";
            return 1;
        }
    }
    catch (const std::invalid_argument& e)
    {
        std::cerr << "Invalid nb-client: '" << line << "' is not a valid integer\n";
        return 1;
    }
    catch (const std::out_of_range& e)
    {
        std::cerr << "Invalid nb-client: '" << line << "' is out of range\n";
        return 1;
    }

    if (nb_client < 1)
    {
        std::cout << "Team is full. Disconnecting...\n";
        return -1;
    }

    std::cout << "[Server] Available slots: " << nb_client << std::endl;

    if (!board.Sock->RecvLine(line))
    {
        std::cerr << "Failed to receive map dimensions\n";
        return 1;
    }

    if (line.empty())
    {
        std::cerr << "Received empty map dimensions\n";
        return 1;
    }

    int x = 0, y = 0;
    std::stringstream ss(line);
    if (!(ss >> x >> y))
    {
        std::cerr << "Error: Failed to parse X and Y from line: '" << line << "'\n";
        return 1;
    }
    if (x <= 0 || y <= 0)
    {
        std::cerr << "Error: Invalid map dimensions: X=" << x << ", Y=" << y << "\n";
        return 1;
    }
    std::string extra;
    if (ss >> extra)
    {
        std::cerr << "Warning: Extra parameters in map dimensions: '" << extra << "'\n";
    }

    std::cout << "[Server] Map dimensions: X=" << x << ", Y=" << y << "\n";

    board.Me.TeamName = teamName;
    board.InitializeMap(x, y);
    
    return 0;
}

void handleServerResponse(Blackboard& board, const std::string& response)
{
    // Obtener el ultimo comando enviado
    const auto& pendingCommands = board.commandHistory.GetPendingCommands();
    if (pendingCommands.empty())
    {
        // No hay comandos pendientes, solo guardar el mensaje. Guardar cuando se recibio?
        board.Messages.push_back(response);
        return;
    }

    CommandType lastCommand = pendingCommands.front().type;

    // Parsear tipo de respuesta
    if (response == "ok")
    {
        switch (lastCommand)
        {
            case CommandType::Advance:
                std::cout << "[Action] Moved forward successfully\n";
                // Actualizar posicion en el Blackboard
                break;
            case CommandType::Right:
                std::cout << "[Action] Turned right successfully\n";
                // Actualizar orientacion en el Blackboard
                break;
            case CommandType::Left:
                std::cout << "[Action] Turned left successfully\n";
                // Actualizar orientacion en el Blackboard
                break;
            case CommandType::Take:
                std::cout << "[Action] Took object successfully\n";
                // Actualizar inventario en el Blackboard
                break;
            case CommandType::Put:
                std::cout << "[Action] Put object successfully\n";
                // Actualizar inventario en el Blackboard
                break;
            case CommandType::Expulse:
                std::cout << "[Action] Expelled player successfully\n";
                break;
            case CommandType::Fork:
                std::cout << "[Action] Fork successful\n";
                break;
            default:
                std::cout << "[Action] Command executed successfully\n";
                break;
        }
    }
    else if (response == "ko")
    {
        switch (lastCommand)
        {
            case CommandType::Advance:
                std::cout << "[Action] Failed to move forward\n";
                break;
            case CommandType::Take:
                std::cout << "[Action] Failed to take object (not present)\n";
                break;
            case CommandType::Put:
                std::cout << "[Action] Failed to put object (not in inventory)\n";
                break;
            case CommandType::Incantation:
                std::cout << "[Action] Incantation failed\n";
                break;
            default:
                std::cout << "[Action] Command failed\n";
                break;
        }
    }
    else if (response.find("elevation en cours") != std::string::npos)
    {
        if (lastCommand == CommandType::Incantation)
        {
            std::cout << "[Action] Incantation in progress...\n";
            // Marcar estado de elevacion en el Blackboard
        }
    }
    else if (response.find('{') != std::string::npos && response.find('}') != std::string::npos)
    {
        // Respuesta con datos (JSON-like o estructura de datos)
        switch (lastCommand)
        {
            case CommandType::See:
                std::cout << "[Action] Processing vision data\n";
                board.HandleVoirResponse(response);
                break;
            case CommandType::Inventory:
                std::cout << "[Action] Processing inventory data\n";
                // Parsear y actualizar inventario en el Blackboard
                break;
            case CommandType::ConnectNbr:
                std::cout << "[Action] Processing connection number\n";
                // Procesar numero de conexiones disponibles
                break;
            default:
                std::cout << "[Action] Received structured data\n";
                board.Messages.push_back(response);
                break;
        }
    }
    else
    {
        // Otras respuestas (por ejemplo, mensajes de broadcast, muerte, etc.)
        if (lastCommand == CommandType::Broadcast)
        {
            std::cout << "[Action] Broadcast sent\n";
        }
        else if (response.find("mort") != std::string::npos)
        {
            std::cout << "[Action] Player died\n";
            // Manejar muerte del jugador
        }
        
        board.Messages.push_back(response);
    }
}

int main()
{
	//TODO parsear argumentos de linea de comando para ip, puerto y nombre de equipo
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
        WaitForDebugAndClean();
        return 1;
    }
    
	// Estructura game deberia tener conexion, current tick counter, 

    Blackboard board(&sock);
	int code = InitServerHandshake(board, "TestTeamName");
    if (code != 0)
    {
        // clean socket and bb? 
        if (code == -1)
        {
            WaitForDebugAndClean();
            return 0;
        }
        std::cerr << "Server handshake failed\n";
        WaitForDebugAndClean();
		return 1;
    }
 
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

		board.commandHistory.AddCommand(bestBid->Type, board.CurrentTick);
        std::cout << "[Client] CMD => " << bestBid->Command << "\n";

        if (!board.Sock->SendLine(bestBid->Command))
            break;

        std::string response;
        while (true)
        {
            if (!board.Sock->RecvLine(response))
                break;

            std::cout << "[Server] RESP <= " << response << "\n";
            handleServerResponse(board, response);
        }
        
        std::this_thread::sleep_for(std::chrono::seconds(10));
    }

    for (auto* a : agents)
        delete a;

    WSACleanup();
    return 0;
}
