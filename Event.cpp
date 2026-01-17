#include "Event.h"

template<typename TArgs>
void Event<TArgs>::Subscribe(Handler handler) {
    handlers.push_back(handler);
}

template<typename TArgs>
void Event<TArgs>::Emit(void* sender, const TArgs& args) {
    for (auto& h : handlers) {
        h(sender, args);
    }
}