#pragma once

#include <array>
#include <random>
#include <vector>

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

constexpr std::array<float, static_cast<size_t>(Resource::Count)> ResourceDensity = {
    0.50f, // Food
    0.30f, // Linemate
    0.15f, // Deraumere
    0.10f, // Sibur
    0.10f, // Mendiane
    0.08f, // Phiras
    0.05f  // Thystame
};

class ResourcesDensityManager {
public:
    ResourcesDensityManager(std::vector<Tile*>& tiles,
                            int width,
                            int height);

    /// Fill the map according to configured densities.
    void Initialize();

    /// Called when a player removes a resource.
    void OnResourcePicked(Resource resource);

    /// Called when a player drops a resource.
    void OnResourceDropped(Resource resource);

    /// Called every server tick.
    void Update();

private:
    void Spawn(Resource resource, int amount);
    Tile* GetRandomTile();

    std::vector<Tile*>& _tiles;

    int _width;
    int _height;

    std::mt19937 _rng;

    std::array<int, static_cast<size_t>(Resource::Count)> _target;
    std::array<int, static_cast<size_t>(Resource::Count)> _current;
};
