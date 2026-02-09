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
    ErrorReporter errors;


    Opt::GetOpt<Opt::Client::Id> opts(
        std::span{Opt::Client::specs},
        std::span{Opt::Client::key_table},
        errors);

    bool ok = opts.parse(argc, argv);
	if (ok == false)
	{
		for (auto& e : errors.all())
			std::cerr << e << std::endl;
		ok = true;
	}
    return (errors.has_errors());
}
