#pragma once
#include <functional>
#include <vector>

template<typename TArgs>
class Event {
    public:
        using Handler = std::function<void(void* sender, const TArgs& args)>;

        void Subscribe(Handler handler) {
            handlers.push_back(handler);
        }

        void Emit(void* sender, const TArgs& args) {
            for (auto& h : handlers) {
                h(sender, args);
            }
        }

    private:
        std::vector<Handler> handlers;
};

