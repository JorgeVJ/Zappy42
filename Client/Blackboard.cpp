#include "Blackboard.h"
#include "ClientLog.h"
#include "UtilityHelper.h"
#include <sstream>
#include <string>
#include <algorithm>


Blackboard::Blackboard() : map(0, 0), CurrentTick(0)
{
    //Initialize food on inventory
	Me.inventory.Add(Resource::Food, 10);
}

void Blackboard::InitializeMap(int x, int y)
{
    map = Map(x, y);
}

void Blackboard::RequestResource(Resource res, int priority)
{
    // Buscar si ya existe una solicitud para este recurso
    for (auto& req : ResourceRequests) {
        if (req.resource == res) {
            // Actualizar prioridad y timestamp
            req.priority = priority;
            req.tickRequested = CurrentTick;
            return;
        }
    }
    
    // Si no existe, crear nueva solicitud
    ResourceRequests.push_back(ResourceRequest(res, priority, CurrentTick));
}

void Blackboard::CleanupOldRequests(int maxAge)
{
    ResourceRequests.erase(
        std::remove_if(
            ResourceRequests.begin(),
            ResourceRequests.end(),
            [this, maxAge](const ResourceRequest& req) {
                return (CurrentTick - req.tickRequested) > maxAge;
            }
        ),
        ResourceRequests.end()
    );
}

int Blackboard::GetRemainingLifeTicks() const
{
	 int food = Me.inventory.Get(Resource::Food);
	return food * TICKS_PER_FOOD;
}

double Blackboard::GetFoodReserveNeed() const
{
	const double maxUsefulFood = 10.0;
	double reserveRatio = static_cast<double>(Me.inventory.Get(Resource::Food)) / maxUsefulFood;
	if (reserveRatio < 0.0)
		reserveRatio = 0.0;
	else if (reserveRatio > 1.0)
		reserveRatio = 1.0;

	return 1.0 - reserveRatio;
}

// Use getLifePercentage or GetHungerneed
double Blackboard::GetLifePercentage() const
{
	int remaining = GetRemainingLifeTicks();
	double percentage = static_cast<double>(remaining) / MAX_REASONABLE_LIFE;
	
	// Clamped entre 0 y 1
	if (percentage < 0.0) return 0.0;
	if (percentage > 1.0) return 1.0;
	return percentage;
}

double Blackboard::GetHungerNeed() const
{
	const double lifePressure = 1.0 - GetLifePercentage();
	const double reservePressure = GetFoodReserveNeed();
	const double combinedNeed = (lifePressure * 0.35) + (reservePressure * 0.65);

	return UtilityHelper::Sigmoid((combinedNeed - 0.5) * 8.0);
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
        int amount = tile->inventory.Get(resource);
        if (amount > 0) {
            Influence* inf = new Influence{
                true,
                resource,
                tile
            };
            this->influenceService.BFSPropagate(tile, resource, inf, 5);
        }
    }
}

Tile* Blackboard::GetPlayerTile() {
    return this->map.GetTile(this->Me.Position.X, this->Me.Position.Y);
}

void Blackboard::HandleVoirResponse(const std::string& response)
{
    std::vector<std::string> cases = ParseVoir(response);
    std::vector<std::pair<int, int>> offsets = GetVoirOffsets(this->Me.Level, this->Me.Orientation);

    Tile* origin = GetPlayerTile();

    for (size_t i = 0; i < cases.size() && i < offsets.size(); ++i) {
        std::pair<int, int> pair = offsets[i];
        int dx = pair.first;
        int dy = pair.second;
        Tile* tile = this->map.GetTile(origin->X + dx, origin->Y + dy);
        if (!tile) continue;

        tile->inventory.Clear();
        this->explorationService.MarkSeen(tile, this->CurrentTick);

        std::stringstream ss(cases[i]);
        std::string res;
        while (ss >> res) {
            tile->inventory.Add(res, 1);
        }

        this->influenceService.CleanSignals(tile);
        PropagateInfluences(tile);
    }
}

void Blackboard::UpdateTick(int ticks)
{
	if (ticks < 0)
	{
		LOG_WARNING("Attempted to update tick with negative value: " << ticks);
		return;
	}

	CurrentTick += ticks;
	LOG_DEBUG("Tick updated to: " << CurrentTick);
}

void Blackboard::ResetTick()
{
    CurrentTick = 0;
    LOG_DEBUG("Tick reset to 0");
}

bool Blackboard::HandleIncantationResponse(const std::string& response)
{
    // Buscar el patron "niveau actuel : "
    const std::string pattern = "niveau actuel : ";
    size_t pos = response.find(pattern);
    
    if (pos == std::string::npos)
    {
        LOG_ERROR("Invalid incantation response format: '" << response << "'");
        return false;
    }
    
    // Extraer la parte despues del patron
    std::string levelStr = response.substr(pos + pattern.length());
    
    // Limpiar espacios en blanco
    levelStr.erase(0, levelStr.find_first_not_of(" \t\n\r"));
    levelStr.erase(levelStr.find_last_not_of(" \t\n\r") + 1);
    
    if (levelStr.empty())
    {
        LOG_ERROR("No level value found in response: '" << response << "'");
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
            LOG_ERROR("Invalid level format: '" << levelStr << "'");
            return false;
        }
        
        // Validar rango de nivel (1-8 segun Zappy)
        if (newLevel < 1 || newLevel > 8)
        {
            LOG_ERROR("Level out of expected range (1-8): " << newLevel);
            return false;
        }
        
        int oldLevel = Me.Level;
        Me.Level = newLevel;

        LOG_CLIENT("Player level updated: " << oldLevel << " -> " << newLevel);

        if (newLevel <= oldLevel)
        {
            LOG_DEBUG("Something went wrong!");
            return false;
        }
        
        return true;
    }
    catch (const std::invalid_argument& e)
    {
        LOG_ERROR("Cannot parse level from: '" << levelStr << "'");
        return false;
    }
    catch (const std::out_of_range& e)
    {
        LOG_ERROR("Level value out of range: '" << levelStr << "'");
        return false;
    }
    
    return false;
}

