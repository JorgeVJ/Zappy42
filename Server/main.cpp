#include <iostream>
#include <string>
#include <sstream>
#include <vector>
#include <cstdlib>
#include <stdexcept>
#include "utils.h"
#include "Game.h"
#include "responses.h"
#include "events.h"
#include "Connection.h" // Discern Windows && Linux includes.
#include "main.h"

#ifdef _WIN32
#pragma comment(lib, "Ws2_32.lib")
#endif

std::vector<Connection*> clients;

int HandlePlayerConnection(Game* game, Connection* client, const std::string& cmd)
{
    game->Players.push_back(client);
    try {
      client->player = new Player();
    }
    catch ( const  std::exception &e)  {
      std::cerr << "Exception STL: " << e.what() << std::endl;
      return (1);
    }
    catch ( ... ) {
      std::cerr << "Exception No STL: " << std::endl;
      return (1);
    }
    if (client->player == nullptr)
      return (1);
    client->player->TeamName = cmd;

    for (auto* monitor : game->Monitors) {
        pnw(client, monitor);
    }

    client->SendLine("1"); // TODO: Enviar valor correcto.
    Map* map = game->WorldMap;
    std::ostringstream ss;
    ss << map->Width << " " << map->Height;
    client->SendLine(ss.str());
	return (0);
}

void HandleCommand(const std::string& cmd, Connection* client)
{
    // Aqu� simulas el server Zappy.
    // Puedes conectarlo luego con tu Map/Tile reales.
    if (cmd == "inventory")
    {
        // Respuesta estilo Zappy
        client->SendLine("{nourriture 12, linemate 1, deraumere 0, sibur 2, mendiane 0, phiras 1, thystame 0}");
    }
    else if (cmd == "voir")
    {
        // Ejemplo MUY simplificado:
        // En el real, son tiles en forma de cono seg�n nivel/orientacion.
        client->SendLine("{nourriture linemate, sibur, phiras phiras,}");
    }
    else if (cmd == "avance")
    {
        client->SendLine("ok");
    }
    else if (cmd == "droite")
    {
        client->SendLine("ok");
    }
    else if (cmd == "gauche")
    {
        client->SendLine("ok");
    }
    else if (cmd.rfind("prend ", 0) == 0)
    {
        // prend nourriture / prend linemate ...
        client->SendLine("ok");
    }
    else if (cmd.rfind("broadcast ", 0) == 0)
    {
        client->SendLine("ok");
    }
    else if (cmd == "fork")
    {
        client->SendLine("ok");
    }
    else if (cmd == "incantation")
    {
        // En el server real, esto depende de condiciones del tile
        client->SendLine("elevation en cours");
    }
    else if (cmd == "msz")
    {
        Map* map = Game::GetInstance()->WorldMap;
        std::ostringstream ss;
        ss << "msz " << map->Width << " " << map->Height;
        client->SendLine(ss.str());
    }
    else if (cmd == "mct") {
        std::string fullMap = GetMCT();
        send(client->Get(), fullMap.c_str(), fullMap.size(), 0);
    }
    else if (cmd == "sgt") {
    }
    else if (cmd == "tna") {
    }
    else if (cmd == "GRAPHIC")
    {
        client->player = nullptr;
        HandleCommand("sgt", client);
        HandleCommand("msz", client);
        HandleCommand("mct", client);
        HandleCommand("tna", client);

        Game* game = Game::GetInstance();
        for (auto* player : game->Players) {
            pnw(player, client);
        }
		for (auto& egg : game->EggRegistry.GetAll()) {
			enw(egg, client);//
		}

        auto itm = std::find(game->Monitors.begin(), game->Monitors.end(), client);
        if (itm == game->Monitors.end()) {
            game->Monitors.push_back(client);
        }

        auto itp = std::find(game->Players.begin(), game->Players.end(), client);
        if (itp != game->Players.end()) {
            game->Players.erase(itp);
        }
    }
    else
    {
        Game* game = Game::GetInstance();
        auto itp = std::find(game->Players.begin(), game->Players.end(), client);
        auto itm = std::find(game->Monitors.begin(), game->Monitors.end(), client);

        if (itp == game->Players.end() && itm == game->Monitors.end()) {
            HandlePlayerConnection(game, client, cmd);
        }
        else {
            client->SendLine("ko");
        }
    }
}

int main()
{
	//Create a Server Class that encapsulate all this.
#ifdef _WIN32
	WSADATA wsaData;
	if (WSAStartup(MAKEWORD(2, 2), &wsaData))
		{
			// printf("WSAStartup failed: %d\n", iResult);
			return (-1);
		}
#endif
	SOCKET rawListen = socket(AF_INET, SOCK_STREAM, 0);
	if (rawListen == INVALID_SOCKET)
		{
#ifdef  _WIN32
			printf("Error at socket(): %ld\n", WSAGetLastError());
			// freeaddrinfo(result);
			WSACleanup();
#elif  defined(__linux__)
			perror("socket failed:");
#endif
			return (1);
		}
    Connection listenSocket(rawListen);
    const int listen_port = 12345;
    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = INADDR_ANY;
    addr.sin_port = htons(listen_port);

    if (bind(listenSocket.Get(), (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR)
    {
#ifdef  _WIN32
      printf("bind failed with error: %d\n", WSAGetLastError());
      // freeaddrinfo(result);
      WSACleanup();
#elif  defined(__linux__)
      perror("bind failed:");
#else
#error "Unexpected OS"
#endif
      return (1);
    }
    if ( listen( listenSocket.Get(), SOMAXCONN ) == SOCKET_ERROR )
    {
#ifdef  _WIN32
      printf( "Listen failed with error: %ld\n", WSAGetLastError() );
      closesocket(listenSocket.Get());
      WSACleanup();
#elif  defined(__linux__)
      perror( "Listen failed: " );
#endif
      return 1;
    }
    std::cout << "Server listening...\n";
    Game::GetInstance();
#ifdef  _WIN32
    system("start ..\\Monitor\\gfx.exe");
#elif  defined(__linux__)
    // system(".\\Monitor\\gfx");
#endif
    while (true)
    {
      fd_set readSet;
      FD_ZERO(&readSet);
      FD_SET(listenSocket.Get(), &readSet);
      SOCKET maxSock = listenSocket.Get();
      for (auto& c : clients)
      {
        FD_SET(c->Get(), &readSet);
        if (c->Get() > maxSock)
          maxSock = c->Get();
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
        if (s == INVALID_SOCKET)
        {
#ifdef  _WIN32
			printf("accept failed: %d\n", WSAGetLastError());
#elif  defined(__linux__)
			perror( "accept failed:" );
#endif
			return (1);
        }
        std::cout << "Client connected!" << std::endl;
        try {
			Connection* client = new Connection(s);
			if (client == nullptr)
				continue;
			client->SendLine("BIENVENUE");
			clients.push_back(client);
        }
        catch( const std::exception &e) {
			std::cerr << "Exception STL: " << e.what() << std::endl;
        }
        catch (...) {
            std::cerr << "Exception No STL: " << std::endl;
        }
      }
      // Clientes existentes
      for (size_t i = 0; i < clients.size();)
      {
        auto& c = clients[i];
        if (FD_ISSET(c->Get(), &readSet))
        {
          std::string msg;
          if (!c->RecvLine(msg))
          {
            std::cout << "Client disconnected" << std::endl ;
            clients.erase(clients.begin() + i);
            continue;
          }

          std::cout << "Received: " << msg << "\n";
          HandleCommand(msg.c_str(), c);
        }
        ++i;
      }
    }
#ifdef  _WIN32
    WSACleanup();
#endif
    return 0;
}
