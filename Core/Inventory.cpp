#include "pch.h"
#include "Inventory.h"

std::map<int, Inventory> Inventory::IncantationRecipes = {
    //  Level	        linemate	deraumere	sibur	mendiane	phiras	thystame
        {1, Inventory({ 1,          0,          0,      0,          0,      0})},
        {2, Inventory({ 1,          1,          1,      0,          0,      0})},
        {3, Inventory({ 2,          0,          1,      0,          2,      0})},
        {4, Inventory({ 1,          1,          2,      0,          1,      0})},
        {5, Inventory({ 1,          2,          1,      3,          0,      0})},
        {6, Inventory({ 1,          2,          3,      0,          1,      0})},
        {7, Inventory({ 2,          2,          2,      2,          2,      1})},
};

void Inventory::InnitMap() {
    // Mapear nombres del servidor a enum Resource
    map = {
        { "nourriture", Resource::Food },
        { "linemate",   Resource::Linemate },
        { "deraumere",  Resource::Deraumere },
        { "sibur",      Resource::Sibur },
        { "mendiane",   Resource::Mendiane },
        { "phiras",     Resource::Phiras },
        { "thystame",   Resource::Thystame }
    };
}

Inventory::Inventory() {
    data.fill(0);
    InnitMap();
}

Inventory::Inventory(std::initializer_list<int> init) {
    data.fill(0);
    int i = 0;
    for (int v : init) {
        if (i < Size())
            data[i++] = v;
    }
    InnitMap();
}

int Inventory::Get(Resource r) {
    return data[static_cast<size_t>(r)];
}

void Inventory::Add(std::string str, int amount) {
    auto it = map.find(str);
    if (it != map.end()) {
        data[static_cast<size_t>(it->second)] = amount;
    }
}

void Inventory::Add(Resource r, int amount) {
    data[static_cast<size_t>(r)] += amount;
}

bool Inventory::Remove(Resource r, int amount) {
    int& value = data[static_cast<size_t>(r)];
    if (value < amount)
        return false;
    value -= amount;
    return true;
}

bool Inventory::Has(Resource r, int amount) {
    return Get(r) >= amount;
}

bool Inventory::Has(const Inventory& required) {
    for (size_t i = 0; i < Size(); ++i) {
        if (data[i] < required.data[i])
            return false;
    }
    return true;
}

void Inventory::Clear() {
    data.fill(0);
}

constexpr size_t Inventory::Size() {
    return static_cast<size_t>(Resource::Count);
}

void Inventory::SetFromServerString(const std::string& str)
{
    Clear(); // vaciamos antes

    // Limpiar curly brackets
    std::string clean = str;
    clean.erase(std::remove(clean.begin(), clean.end(), '{'), clean.end());
    clean.erase(std::remove(clean.begin(), clean.end(), '}'), clean.end());

    std::stringstream ss(clean);
    std::string token;

    while (std::getline(ss, token, ',')) {
        // Quitar espacios al inicio
        token.erase(0, token.find_first_not_of(" \t\n\r"));

        std::stringstream item(token);
        std::string name;
        int value;

        item >> name >> value;

        auto it = map.find(name);
        if (it != map.end()) {
            data[static_cast<size_t>(it->second)] = value;
        }
    }
}

std::string Inventory::ResourceToString(Resource resource)
{
    std::map<Resource, std::string> map = {
        { Resource::Food, "nourriture" },
        { Resource::Linemate, "linemate" },
        { Resource::Deraumere, "deraumere" },
        { Resource::Sibur, "sibur" },
        { Resource::Mendiane, "mendiane" },
        { Resource::Phiras, "phiras" },
        { Resource::Thystame, "thystame" }
    };

    return map[resource];
}
