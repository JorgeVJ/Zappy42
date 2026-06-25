#pragma once

#include <vector>
#include <string_view>
#include <span>
#include "GetOpt.h"

namespace Opt {
	namespace Server {
		enum class Id {
			Port,
			Width,
			Height,
			Teams,
			Clients,
			Time,
		};

		// Forward declarations - defined in ServerOptions.cpp
    constexpr Spec specs[] = {
			{ port_keys, Arity::One, RepeatPolicy::Reject },
			{ width_keys, Arity::One, RepeatPolicy::Reject },
			{ height_keys, Arity::One, RepeatPolicy::Reject },
			{ teams_keys, Arity::OneOrMore, RepeatPolicy::Accumulate },
			{ clients_keys, Arity::One, RepeatPolicy::Reject },
			{ time_keys, Arity::One, RepeatPolicy::Reject },
		};

		constexpr KeyEntry<Server::Id> key_table[] = {
			{ "-p", Opt::Server::Id::Port },
			{ "-x", Opt::Server::Id::Width },
			{ "-y", Opt::Server::Id::Height },
			{ "-n", Opt::Server::Id::Teams },
			{ "-c", Opt::Server::Id::Clients },
			{ "-t", Opt::Server::Id::Time },
		};

		struct Args {
			int port;
			int width;
			int height;
			int time;
			int players;
			std::vector<std::string_view> teams;
		};
	}
}
