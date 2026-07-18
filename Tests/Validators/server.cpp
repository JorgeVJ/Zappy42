#include <iostream>
#include "Result.h"
#include "GetOpt.h"
#include "servervalidators.h"
#include "serveroptions.h"
#include "ArgValidation.h"

int main(int argc, char** argv) {

  Opt::GetOpt<Opt::Server::Id> opts(std::span{Opt::Server::specs},
                                    std::span{Opt::Server::key_table});
  std::vector<std::string> errors;
  //Parsing
  bool ok = opts.parse(argc, argv, &errors);
	if (ok == false)
	{
		std::cerr << "Parsing Error" << std::endl;
		for (auto& e : errors)
			std::cerr << e << std::endl;
		return (false);
  }
	ok &= validate_arity(opts.values, opts.specs, &errors);
  // Validation
  Opt::Server::Args args = {};
	errors.clear();
  auto validationResult = ArgValidation::ValidateServerArgs(opts, args, &errors);

	if (!validationResult.Ok)
	{
		std::cerr << "Argument Validation Error:" << std::endl;
		for (const auto& e : errors)
			std::cerr << "  " << e << std::endl;
		std::cerr << "  " << validationResult.Message << std::endl;
		return 1;
	}
	return (ok == false || !errors.empty());
}
