#include "ResourcesDensityManager.h"

ResourcesDensityManager::ResourcesDensityManager(
    std::vector<Tile*>& tiles,
    int width,
    int height)
    : _tiles(tiles),
      _width(width),
      _height(height),
      _rng(std::random_device{}())
{
 	const int area = width * height;

	for (size_t i = 0; i < target.size(); ++i)
	{
		_target[i] = static_cast<int>(area * ResourceDensity[i]);
		Resource r = static_cast<Resource>(i);
        Spawn(r, _target[i]);
	}
	_current = _target;
}

void ResourcesDensityManager::Update()
{
    for (size_t i = 0; i < Inventory::Size(); ++i)
    {
        if (_current[i] >= _target[i])
            continue;

        int missing = _target[i] - _current[i];

        Spawn(static_cast<Resource>(i), missing);

        _current[i] += missing;
    }
}

void ResourcesDensityManager::OnResourcePicked(Resource resource)
{
    --_current[static_cast<size_t>(resource)];
}

Tile* ResourcesDensityManager::GetRandomTile()
{
    std::uniform_int_distribution<size_t> dist(0, _tiles.size() - 1);
    return _tiles[dist(_rng)];
}

void ResourcesDensityManager::Spawn(Resource resource, int amount)
{
    for (int i = 0; i < amount; ++i)
         GetRandomTile()->inventory.Add(resource);
 }

void ResourcesDensityManager::OnResourcesConsumed(const Inventory& inventory)
{
    for (size_t i = 0; i < Inventory::Size(); ++i)
         _currentResources[i] -= inventory.Get(static_cast<Resource>(i));

}
