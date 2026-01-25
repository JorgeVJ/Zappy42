#include "pch.h"
#include "TileDataRegistry.h"

template<typename T>
T* TileDataRegistry<T>::Get(Tile* tile) const {
    return data[tile];
}

template<typename T>
T* TileDataRegistry<T>::TryGet(Tile* tile) const {
    auto it = data.find(tile);
    return (it == data.end()) ? nullptr : &it->second;
}

template<typename T>
void TileDataRegistry<T>::Add(Tile* tile, T data)
{
    this->data[tile] = data;
}

template<typename T>
void TileDataRegistry<T>::Clear(Tile* tile)
{
    this->data.erase(tile);
}
