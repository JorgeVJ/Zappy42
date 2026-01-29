#pragma once
#include "Tile.h"
#include <map>

template <typename T>
class TileDataRegistry
{
    public:
        T* Get(Tile* tile) {
            return &data[tile];
        }

        T* TryGet(Tile* tile)
        {
            auto it = data.find(tile);
            if (it == data.end()) return nullptr;
            return &it->second;
        }

        const T* TryGet(Tile* tile) const
        {
            auto it = data.find(tile);
            if (it == data.end()) return nullptr;
            return &it->second;
        }

        void Add(Tile* tile, T& data)
        {
            this->data[tile] = data;
        }

        void Clear(Tile* tile)
        {
            this->data.erase(tile);
        }

        std::vector<T*> GetAll()
        {
            std::vector<T*> result;
            result.reserve(data.size());

            for (auto& pair : data)
            {
                result.push_back(&pair.second);
            }

            return result;
        }

    private:
        std::map<Tile*, T> data;
};
