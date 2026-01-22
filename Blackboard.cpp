#include "Blackboard.h"

double Blackboard::GetHungerNeed() {
	double value = 40.0 / Inventory.Get(Resource::Food);

	if (value < 0.0)
		return 0.0;
	else if (value > 1.0)
		return 1.0;

	return value;
}

Blackboard::Blackboard(int x, int y) : Map(x, y)
{
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
            tile->Signals.push_back(inf);
            Map.BFSPropagate(tile, resource, inf, 5);
        }
    }
}

Tile* Blackboard::GetPlayerTile() {
    return Map.GetTile(Position.X, Position.Y);
}

void Blackboard::HandleVoirResponse(const std::string& response)
{
    std::vector<std::string> cases = ParseVoir(response);
    std::vector<std::pair<int, int>> offsets = GetVoirOffsets(Level, PlayerDirection);

    Tile* origin = GetPlayerTile();
    
    for (size_t i = 0; i < cases.size() && i < offsets.size(); ++i) {
        std::pair<int, int> pair = offsets[i];
        int dx = pair.first;
        int dy = pair.second;
        Tile* tile = Map.GetTile(origin->X + dx, origin->Y + dy);
        if (!tile) continue;

        tile->Inventory.Clear();
        tile->EverSeen = true;

        std::stringstream ss(cases[i]);
        std::string res;
        while (ss >> res) {
            tile->Inventory.Add(res, 1);
        }

        tile->CleanInfluences();
        PropagateInfluences(tile);
    }
}

