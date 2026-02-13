#include <iostream>
#include "GetOpt.h"
//#include "Validators.h"

namespace Opt {
	namespace Client {
		enum class Id {
			Port,
			Teams,
			Host,
		};

		constexpr Spec specs[] = {
			{ port_keys, Arity::One, RepeatPolicy::Reject },
			{ teams_keys, Arity::One, RepeatPolicy::Reject },
			{ host_keys, Arity::ZeroOrOne, RepeatPolicy::Reject },
		};

		constexpr KeyEntry<Id> key_table[] = {
			{ "-p",       Id::Port },
			{ "-n",       Id::Teams },
			{ "-h",       Id::Host },
		};
	}
}

int main(int argc, char** argv) {
	std::vector<std::string_view> errors;


    Opt::GetOpt<Opt::Client::Id> opts(
        std::span{Opt::Client::specs},
        std::span{Opt::Client::key_table});

    bool ok = opts.parse(argc, argv, &errors);
	if (ok == false)
	{
		for (auto& e : errors)
			std::cerr << e << std::endl;
		ok = true;
	}
	ok &= validate_arity(opts.values, opts.specs, &errors);
	return (ok == false || errors.size());
}
