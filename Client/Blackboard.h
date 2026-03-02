#pragma once
#include <vector>
#include "Bid.h"
#include "Inventory.h"
#include "Tile.h"
#include "Point.h"
#include <memory>
#include "Map.h"
#include "InfluenceService.h"
#include "ExplorationService.h"
#include <Player.h>
#include <CommandHistory.h>
#include "Connection.h"
#include "MessageEntry.h"

/// <summary>
/// Solicitud de busqueda de recursos
/// </summary>
struct ResourceRequest {
    Resource resource;
    int priority;      // Que tan urgente es (0-100)
    int tickRequested; // Cuando se solicito
    
    ResourceRequest(Resource res, int prio, int tick)
        : resource(res), priority(prio), tickRequested(tick) {}
};

/// <summary>
/// All the information needed to make decisions.
/// </summary>
class Blackboard
{
	public:
		// Game constants
		static constexpr int TICKS_PER_FOOD = 126;
		
		/// <summary>
		/// Sum of all Command Ticks.
		/// </summary>
		Map map;
		int CurrentTick;
		int ConnectNbr = 0;

		// Number of players we are breeding. Do we start with just one connection? Shoud we send connectNbr command from the start to know how many players we have inmediately?
		int TeamNbr = 0;

		Player Me;

		/// <summary>
		/// Contiene las pujas de los agentes para el tick actual. Se limpia cada tick despues de elegir la mejor puja.
		/// </summary>
		std::vector<Bid> Bids;

		/// <summary>
		/// Contain the Broadcasts from other players.
		/// </summary>
		std::vector<MessageEntry> Messages;
		
		/// <summary>
		/// Recursos que otros agentes necesitan encontrar.
		/// Incluye comida (solicitada por AgentFeeder) y minerales (por AgentStoner)
		/// </summary>
		std::vector<ResourceRequest> ResourceRequests;
		
		/// <summary>
		/// Servicio para manejar las influencias de los recursos en el mapa. 
		/// </summary>
		InfluenceService influenceService;
		ExplorationService explorationService;

		CommandHistory commandHistory;

		Blackboard(); // constructor por defecto

		void InitializeMap(int x, int y);
		
		/// <summary>
		/// Agrega o actualiza una solicitud de recurso
		/// </summary>
		void RequestResource(Resource res, int priority);
		
		/// <summary>
		/// Limpia solicitudes antiguas (mas de maxAge ticks)
		/// </summary>
		void CleanupOldRequests(int maxAge = 1000);
		
		/// <summary>
		/// Incrementa el CurrentTick por la cantidad especificada
		/// </summary>
		/// <param name="ticks">Cantidad de ticks a incrementar (debe ser positivo)</param>
		void UpdateTick(int ticks);
		
		/// <summary>
		/// Resetea el CurrentTick a 0
		/// </summary>
		void ResetTick();
		
		/// <summary>
		/// Calcula los ticks de vida restantes basandose en la comida actual
		/// </summary>
		/// <returns>Ticks de vida restantes</returns>
		int GetRemainingLifeTicks() const;
		
		/// <summary>
		/// Obtiene el porcentaje de vida restante (0.0 a 1.0)
		/// </summary>
		/// <returns>Porcentaje de vida entre 0.0 y 1.0</returns>
		double GetLifePercentage() const;
		
		/// <summary>
		/// Calcula la necesidad de comida como valor de urgencia
		/// </summary>
		/// <returns>Valor de urgencia entre 0.0 (sin hambre) y 1.0 (muerte inminente)</returns>
		double GetHungerNeed();
		
		/// <summary>
		/// Procesa la respuesta del comando "voir" y actualiza el mapa
		/// </summary>
		void HandleVoirResponse(const std::string& response);
		
		/// <summary>
		/// Procesa la respuesta de un ritual de incantacion exitoso
		/// </summary>
		bool HandleIncantationResponse(const std::string& response);
		
		/// <summary>
		/// Obtiene los offsets relativos de las casillas visibles segun el nivel y orientacion
		/// </summary>
		std::vector<std::pair<int, int>> GetVoirOffsets(int level, Direction dir);
		
		/// <summary>
		/// Propaga las influencias de recursos desde un tile especifico
		/// </summary>
		void PropagateInfluences(Tile* tile);
		
		/// <summary>
		/// Obtiene el tile donde esta ubicado el jugador actualmente
		/// </summary>
		Tile* GetPlayerTile();
};
