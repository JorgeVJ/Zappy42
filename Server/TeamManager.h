#pragma once
#include <iostream>   //ostream
#include <algorithm> //find_if
#include <string>
#include "servervalidators.h"

struct Team {
    std::string name;           // Team name
    size_t playerCount = 0;     // Player count

    bool IsFull() const;
    bool HasSpace() const;      // Check if < 6
    bool AddPlayer();           // Increment if space
    bool RemovePlayer();        // Decrement if > 0
    size_t GetAvailableSlots() const ; // Get 0-6
};
  std::ostream& operator<<(std::ostream& os, const Team& team);

class TeamManager {
  public:
    TeamManager() = default;
    explicit TeamManager(const std::vector<std::string>& teamNames);
    ~TeamManager() = default;

    // Find & query
    Team* FindTeam(const std::string& name);
    const Team* FindTeam(const std::string& name) const;
    bool TeamExists(const std::string& name) const;
    bool TeamHasSpace(const std::string& name) const;
    bool CanLayEgg(const std::string& name) const;
    bool TeamIsFull(const std::string& teamName) const;
    size_t GetAvailableSlots(const std::string& teamName) const;
    std::vector<Team>& GetTeams();
    const std::vector<Team>& GetTeams() const;
    // Modify
    void AddTeam(const std::string& teamName);
    bool AddPlayerToTeam(const std::string& name);
    bool RemovePlayerFromTeam(const std::string& name);

    // Statistics
    size_t GetTeamCount() const;
    size_t GetPlayerCount(const std::string& name) const;
    size_t GetTotalPlayerCount() const;
    void PrintStatus() const;
    
  private:    
    std::vector<Team> m_teams;  // Single vector with all data


};
