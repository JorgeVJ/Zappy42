#include "TeamManager.h"

bool Team::IsFull() const
{
  return (playerCount >= Validators::Server::Player::Max_per_team);
}
 
/// <summary>
/// Check if team has space for more players
/// </summary>
/// <returns>true if playerCount < Max_per_team, false otherwise</returns>
bool Team::HasSpace() const
{
  return (playerCount < Validators::Server::Player::Max_per_team);
}
 
/// <summary>
/// Get available slots in team
/// </summary>
/// <returns>Number of available slots (0 if full)</returns>
size_t Team::GetAvailableSlots() const
{
  return (IsFull() ? 0 : (Validators::Server::Player::Max_per_team - playerCount));
}
 
/// <summary>
/// Add a player to the team
/// </summary>
/// <returns>true if player was added, false if team is full</returns>
bool Team::AddPlayer()
{
  if (IsFull())
    return (false);
 
  playerCount++;
  return (true);
}
 
/// <summary>
/// Remove a player from the team
/// </summary>
/// <returns>true if player was removed, false if no players</returns>
bool Team::RemovePlayer()
{
  if (playerCount == 0)
    return (false);
 
  playerCount--;
  return (true);
}

std::ostream& operator<<(std::ostream& os, const Team& team)
{
  return (os << team.name << " (" << std::to_string(team.playerCount) << "/" 
          << std::to_string(Validators::Server::Player::Max_per_team) << ")");
}

// Team Manager Class
TeamManager::TeamManager(const std::vector<std::string_view>& teamNames)
{
  for (const auto& name : teamNames)
    {
      m_teams.emplace_back(name);
    }
}
 
/// <summary>
/// Add a team to the manager
/// </summary>
/// <param name="teamName">Name of the team</param>
void TeamManager::AddTeam(std::string_view teamName)
{
  if (!FindTeam(teamName))
    {
      m_teams.emplace_back(teamName);
    }
}
 
/// <summary>
/// Find a team by name
/// </summary>
/// <param name="teamName">Name of the team to find</param>
/// <returns>Pointer to Team if found, nullptr otherwise</returns>
Team* TeamManager::FindTeam(std::string_view teamName)
{
  auto it = std::find_if(m_teams.begin(), m_teams.end(),
                         [teamName](const Team& team) {
                           return (team.name == teamName);
                         });
 
  return ((it != m_teams.end()) ? &(*it) : nullptr);
}
 
/// <summary>
/// Find a team by name (const version)
/// </summary>
const Team* TeamManager::FindTeam(std::string_view teamName) const
{
  auto it = std::find_if(m_teams.begin(), m_teams.end(),
                         [teamName](const Team& team) {
                           return (team.name == teamName);
                         });
 
  return ((it != m_teams.end()) ? &(*it) : nullptr);
}
 
/// <summary>
/// Check if team exists
/// </summary>
/// <param name="teamName">Name of the team</param>
/// <returns>true if team exists, false otherwise</returns>
bool TeamManager::TeamExists(std::string_view teamName) const
{
  return (FindTeam(teamName) != nullptr);
}
 
/// <summary>
/// Check if team has space
/// </summary>
/// <param name="teamName">Name of the team</param>
/// <returns>true if team has space, false if full or doesn't exist</returns>
bool TeamManager::TeamHasSpace(std::string_view teamName) const
{
  const Team* team = FindTeam(teamName);
  return (team != nullptr && team->HasSpace());
}
 
/// <summary>
/// Check if team is full
/// </summary>
/// <param name="teamName">Name of the team</param>
/// <returns>true if team is full, false otherwise</returns>
bool TeamManager::TeamIsFull(std::string_view teamName) const
{
  const Team* team = FindTeam(teamName);
  return (team != nullptr && team->IsFull());
}
 
/// <summary>
/// Get available slots in a team
/// </summary>
/// <param name="teamName">Name of the team</param>
/// <returns>Number of available slots (0 if full or doesn't exist)</returns>
size_t TeamManager::GetAvailableSlots(std::string_view teamName) const
{
  const Team* team = FindTeam(teamName);
  return (team != nullptr ? team->GetAvailableSlots() : 0);
}
 
/// <summary>
/// Get player count for a team
/// </summary>
/// <param name="teamName">Name of the team</param>
/// <returns>Number of players (0 if team doesn't exist)</returns>
size_t TeamManager::GetPlayerCount(std::string_view teamName) const
{
  const Team* team = FindTeam(teamName);
  return (team != nullptr ? team->playerCount : 0);
}
 
/// <summary>
/// Add a player to a team
/// </summary>
/// <param name="teamName">Name of the team</param>
/// <returns>true if player was added, false if team full or doesn't exist</returns>
bool TeamManager::AddPlayerToTeam(std::string_view teamName)
{
  Team* team = FindTeam(teamName);
  return (team != nullptr && team->AddPlayer());
}
 
/// <summary>
/// Remove a player from a team
/// </summary>
/// <param name="teamName">Name of the team</param>
/// <returns>true if player was removed, false if no players or team doesn't exist</returns>
bool TeamManager::RemovePlayerFromTeam(std::string_view teamName)
{
  Team* team = FindTeam(teamName);
  return (team != nullptr && team->RemovePlayer());
}
 
/// <summary>
/// Check if a player can lay an egg (team has space)
/// </summary>
/// <param name="teamName">Name of the team</param>
/// <returns>true if team has space for new player, false otherwise</returns>
bool TeamManager::CanLayEgg(std::string_view teamName) const
{
  return (TeamHasSpace(teamName));
}
 
/// <summary>
/// Get all teams
/// </summary>
/// <returns>Reference to the teams vector</returns>
std::vector<Team>& TeamManager::GetTeams()
{
  return (m_teams);
}
 
/// <summary>
/// Get all teams (const version)
/// </summary>
const std::vector<Team>& TeamManager::GetTeams() const
{
  return (m_teams);
}
 
/// <summary>
/// Get number of teams
/// </summary>
/// <returns>Total number of teams</returns>
size_t TeamManager::GetTeamCount() const
{
  return (m_teams.size());
}
 
/// <summary>
/// Get total player count across all teams
/// </summary>
/// <returns>Sum of all player counts</returns>
size_t TeamManager::GetTotalPlayerCount() const
{
  size_t total = 0;
  for (const auto& team : m_teams)
    {
      total += team.playerCount;
    }
  return (total);
}
 
/// <summary>
/// Print all teams and their status
/// </summary>
void TeamManager::PrintStatus() const
{
  std::cout << "Team Status:" << std::endl;
  for (const auto& team : m_teams)
    {
      std::cout << "  " << team;
      if (team.IsFull())
        std::cout << " [FULL]";
      std::cout << std::endl;
    }
  std::cout << "  Total players: " << GetTotalPlayerCount() << std::endl;
}

