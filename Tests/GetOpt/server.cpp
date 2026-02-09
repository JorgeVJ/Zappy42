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
			{ port_keys, Arity::One, Validators::port, RepeatPolicy::Reject },
			{ width_keys, Arity::One, nullptr, RepeatPolicy::Reject },  //Validate > 1
			{ height_keys, Arity::One, Validators::height, RepeatPolicy::Reject }, //Validate > 1
			{ teams_keys, Arity::OneOrMore, Validators::unique_strings, RepeatPolicy::Accumulate },
			{ clients_keys, Arity::One, nullptr, RepeatPolicy::Reject },  // Validate > 1.
			{ time_keys, Arity::One, nullptr, RepeatPolicy::Reject },//Validate > 1
		};

		constexpr KeyEntry<Opt> key_table[] = {
			{ "-p", Opt::Port },
			{ "-x", Opt::Width },
			{ "-y", Opt::Height },
			{ "-n", Opt::Teams },
			{ "-c", Opt::Clients },
			{ "-t", Opt::Time },
		};
	}
}
int main(int argc, char** argv) {
    ErrorReporter errors;


    Opt::GetOpt<Opt::Server::Opt> opts(
        std::span{Opt::Server::specs},
        std::span{Opt::Server::key_table},
        errors);

    bool ok = opts.parse(argc, argv);

	if (ok == false)
	{
      std::cerr << "Parsing Error" << std::endl;
      for (auto& e : errors.all())
		  std::cerr << e << std::endl;
      ok = true;
    }
	return (0);
}
