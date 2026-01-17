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

class Inventory {
    public:
        Inventory();
        Inventory(std::initializer_list<int> init);
        int Get(Resource r);
        void Add(std::string str, int amount);
        void Add(Resource r, int amount = 1);
        bool Remove(Resource r, int amount = 1);
        bool Has(Resource r, int amount = 1);
        bool Has(const Inventory& required);
        void Clear();
        static constexpr size_t Size();
        void SetFromServerString(const std::string& str);
        static std::map<int, Inventory> IncantationRecipes;
        static std::string ResourceToString(Resource resource);

    private:
        void InnitMap();
        std::unordered_map<std::string, Resource> map;
        std::array<int, static_cast<size_t>(Resource::Count)> data;
};
