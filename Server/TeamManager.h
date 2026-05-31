#pragma once
#include "servervalidators.h"

struct Team {
    std::string name;           // Team name
    size_t playerCount = 0;     // Player count

    bool IsFull() const;        // Check if Server::Validators:Pl
    bool HasSpace() const;      // Check if < 6
    bool AddPlayer();           // Increment if space
    bool RemovePlayer();        // Decrement if > 0
    size_t GetAvailableSlots(); // Get 0-6
    std::string GetStatusString(); // "TeamA (3/6)"
};

class TeamManager {
    std::vector<Team> m_teams;  // Single vector with all data

    // Find & query
    Team* FindTeam(std::string_view name);
    bool TeamExists(std::string_view name);
    bool TeamHasSpace(std::string_view name);
    bool CanLayEgg(std::string_view name);

    // Modify
    bool AddPlayerToTeam(std::string_view name);
    bool RemovePlayerFromTeam(std::string_view name);

    // Statistics
    size_t GetPlayerCount(std::string_view name);
    size_t GetTotalPlayerCount();
    void PrintStatus();
};
