#include <iostream>
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
	}
}

int main(int argc, char** argv) {
	std::vector<std::string_view> errors;
    Opt::GetOpt<Opt::Server::Id> opts(
        std::span{Opt::Server::specs},
        std::span{Opt::Server::key_table});

    bool ok = opts.parse(argc, argv, &errors);

	if (ok == false)
	{
      std::cerr << "Parsing Error" << std::endl;
      for (auto& e : errors)
		  std::cerr << e << std::endl;
      ok = true;
    }
	ok &= validate_arity(opts.values, opts.specs, &errors);
	return (ok == false || errors.size());
}
