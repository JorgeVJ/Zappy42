#pragma once
#include <array>
#include <initializer_list>
#include <unordered_map>
#include <sstream>
#include <string>
#include <map>

enum class Resource {
    Food,
    Linemate,
    Deraumere,
    Sibur,
    Mendiane,
    Phiras,
    Thystame,
    Count
};

// Forward declaration
struct IncantationRecipe;

class Inventory {
    public:
        Inventory();
        Inventory(std::initializer_list<int> init);
        int Get(Resource r) const;
        void Add(std::string str, int amount);
        void Add(Resource r, int amount = 1);
        bool Remove(std::string str, int amount);
        bool Remove(Resource r, int amount = 1);
        bool Has(Resource r, int amount = 1) const;
        bool Has(const Inventory& required) const;
        void Clear();

        void SetFromServerString(const std::string& str);
        
        /// Imprime el inventario en formato tabla para debugging
        void Print(const std::string& title = "Inventory") const;

        /// Devuelve una representacion en string del inventario
        std::string ToString() const;
        
        static std::map<int, IncantationRecipe> IncantationRecipes;
        static std::string ResourceToString(Resource resource);
        static constexpr size_t Size() {
          return static_cast<size_t>(Resource::Count);
        }
    private:
        void InnitMap();
        std::unordered_map<std::string, Resource> map;
        std::array<int, static_cast<size_t>(Resource::Count)> data;
};

/// <summary>
/// Receta para realizar una incantacion y subir de nivel
/// </summary>
struct IncantationRecipe
{
    Inventory RequiredResources;
    int RequiredPlayers;

    IncantationRecipe(Inventory resources, int players)
        : RequiredResources(resources), RequiredPlayers(players)
    {
    }
};
