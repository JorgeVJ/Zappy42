#include "Blackboard.h"
#include <iostream>
#include <sstream>
#include <string>

Blackboard::Blackboard() : Map(0, 0), CurrentTick(0)
{
}

void Blackboard::InitializeMap(int x, int y)
{
	Map = ::Map(x, y);
}

double Blackboard::GetHungerNeed() {
	double value = 40.0 / Me.Inventory.Get(Resource::Food);

	if (value < 0.0)
		return 0.0;
	else if (value > 1.0)
		return 1.0;

	return value;
}

std::vector<std::string> ParseVoir(const std::string& str)
{
    std::vector<std::string> cases;
    std::string content = str.substr(1, str.size() - 2); // quitar { }

    std::stringstream ss(content);
    std::string item;

    while (std::getline(ss, item, ',')) {
        // trim
        item.erase(0, item.find_first_not_of(' '));
        item.erase(item.find_last_not_of(' ') + 1);
        cases.push_back(item);
    }

    return cases;
}

std::vector<std::pair<int, int>> Blackboard::GetVoirOffsets(int level, Direction dir)
{
    std::vector<std::pair<int, int>> offsets;

    for (int d = 0; d <= level; ++d) {
        for (int i = -d; i <= d; ++i) {
            switch (dir) {
            case Direction::North: offsets.emplace_back(i, -d); break;
            case Direction::South: offsets.emplace_back(-i, d); break;
            case Direction::East:  offsets.emplace_back(d, i); break;
            case Direction::West:  offsets.emplace_back(-d, -i); break;
            default: break;
            }
        }
    }
    return offsets;
}

void Blackboard::PropagateInfluences(Tile* tile)
{
    for (size_t r = 0; r < Inventory::Size(); ++r) {
        Resource resource = static_cast<Resource>(r);
        int amount = tile->Inventory.Get(resource);
        if (amount > 0) {
            Influence* inf = new Influence{
                true,
                resource,
                tile
            };
            InfluenceService.BFSPropagate(tile, resource, inf, 5);
        }
    }
}

Tile* Blackboard::GetPlayerTile() {
    return Map.GetTile(Me.Position.X, Me.Position.Y);
}

void Blackboard::HandleVoirResponse(const std::string& response)
{
    std::vector<std::string> cases = ParseVoir(response);
    std::vector<std::pair<int, int>> offsets = GetVoirOffsets(Me.Level, Me.Orientation);

    Tile* origin = GetPlayerTile();
    
    for (size_t i = 0; i < cases.size() && i < offsets.size(); ++i) {
        std::pair<int, int> pair = offsets[i];
        int dx = pair.first;
        int dy = pair.second;
        Tile* tile = Map.GetTile(origin->X + dx, origin->Y + dy);
        if (!tile) continue;

        tile->Inventory.Clear();
        ExplorationService.MarkSeen(tile, CurrentTick);

        std::stringstream ss(cases[i]);
        std::string res;
        while (ss >> res) {
            tile->Inventory.Add(res, 1);
        }

        InfluenceService.CleanSignals(tile);
        PropagateInfluences(tile);
    }
}

void Blackboard::UpdateTick(int ticks)
{
	if (ticks < 0)
	{
		std::cerr << "[Warning] Attempted to update tick with negative value: " << ticks << "\n";
		return;
	}
	
	CurrentTick += ticks;
	std::cout << "[Debbug] Tick updated to: " << CurrentTick << "\n";
}

void Blackboard::ResetTick()
{
    CurrentTick = 0;
    std::cout << "[Debbug] Tick reset to 0\n";
}

bool Blackboard::HandleIncantationResponse(const std::string& response)
{
    // Buscar el patron "niveau actuel : "
    const std::string pattern = "niveau actuel : ";
    size_t pos = response.find(pattern);
    
    if (pos == std::string::npos)
    {
        std::cerr << "[Error] Invalid incantation response format: '" << response << "'\n";
        return false;
    }
    
    // Extraer la parte despues del patron
    std::string levelStr = response.substr(pos + pattern.length());
    
    // Limpiar espacios en blanco
    levelStr.erase(0, levelStr.find_first_not_of(" \t\n\r"));
    levelStr.erase(levelStr.find_last_not_of(" \t\n\r") + 1);
    
    if (levelStr.empty())
    {
        std::cerr << "[Error] No level value found in response: '" << response << "'\n";
        return false;
    }
    
    // Parsear el nivel
    try
    {
        size_t parsePos = 0;
        int newLevel = std::stoi(levelStr, &parsePos);
        
        // Verificar que se parseo toda la string
        if (parsePos != levelStr.length())
        {
            std::cerr << "[Error] Invalid level format: '" << levelStr << "'\n";
            return false;
        }
        
        // Validar rango de nivel (1-8 segun Zappy)
        if (newLevel < 1 || newLevel > 8)
        {
            std::cerr << "[Error] Level out of expected range (1-8): " << newLevel << "\n";
            return false;
        }
        
        int oldLevel = Me.Level;
        Me.Level = newLevel;
        
        std::cout << "[Blackboard] Player level updated: " << oldLevel << " -> " << newLevel << "\n";
        
        if (newLevel <= oldLevel)
        {
            std::cout << "[Debbug] Something went wrong!\n";
            return false;
        }
        
        return true;
    }
    catch (const std::invalid_argument& e)
    {
        std::cerr << "[Error] Cannot parse level from: '" << levelStr << "'\n";
        return false;
    }
    catch (const std::out_of_range& e)
    {
        std::cerr << "[Error] Level value out of range: '" << levelStr << "'\n";
        return false;
    }
    
    return false;
}

