#include <iostream>
#include "GetOpt.h"
#include "serveroptions.h"

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
