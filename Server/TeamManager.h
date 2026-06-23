#pragma once
#include <iostream>   //ostream
#include <algorithm> //find_if
#include <string_view>
#include "servervalidators.h"

struct Team {
    std::string_view name;           // Team name
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
    explicit TeamManager(const std::vector<std::string_view>& teamNames);
    ~TeamManager() = default;
    
    // Find & query
    Team* FindTeam(std::string_view name);
    const Team* FindTeam(std::string_view name) const;
    bool TeamExists(std::string_view name) const;
    bool TeamHasSpace(std::string_view name) const;
    bool CanLayEgg(std::string_view name) const;
    bool TeamIsFull(std::string_view teamName) const;
    size_t GetAvailableSlots(std::string_view teamName) const;
    std::vector<Team>& GetTeams();
    const std::vector<Team>& GetTeams() const;
    // Modify
    void AddTeam(std::string_view teamName);
    bool AddPlayerToTeam(std::string_view name);
    bool RemovePlayerFromTeam(std::string_view name);
    
    // Statistics
    size_t GetTeamCount() const;
    size_t GetPlayerCount(std::string_view name) const;
    size_t GetTotalPlayerCount() const;
    void PrintStatus() const;
    
  private:    
    std::vector<Team> m_teams;  // Single vector with all data


};
