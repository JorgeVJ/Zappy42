#include <functional>
#include <vector>

template<typename TArgs>
class Event {
    public:
        using Handler = std::function<void(void* sender, const TArgs& args)>;
        void Subscribe(Handler handler);
        void Emit(void* sender, const TArgs& args);

    private:
        std::vector<Handler> handlers;
};

