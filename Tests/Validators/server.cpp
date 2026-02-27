#include <iostream>
#include "GetOpt.h"
#include "servervalidators.h"
#include "serveroptions.h"

int main(int argc, char** argv) {

  Opt::GetOpt<Opt::Server::Id> opts(std::span{Opt::Server::specs},
                                    std::span{Opt::Server::key_table});
  std::vector<std::string_view> errors;
  //Parsing
  bool ok = opts.parse(argc, argv, &errors);
	if (ok == false)
	{
		std::cerr << "Parsing Error" << std::endl;
		for (auto& e : errors)
			std::cerr << e << std::endl;
		ok = true;
    }
	ok &= validate_arity(opts.values, opts.specs, &errors);
	errors.clear();
  // Validation
  Opt::Server::Args args = {};

	if (ok == true) {
		auto &val = opts.values[static_cast<size_t>(Opt::Server::Id::Port)].values;
		auto port = Validators::port(val, &errors);
		ok &= port.Ok;
		if (ok)
			args.port = port.Value;
	}
	if (ok == true) {
		auto width = Validators::Server::valid_heigth_or_weight(opts.values[static_cast<size_t>(Opt::Server::Id::Width)].values, &errors);
		ok &= width.Ok;
		if (ok)
			args.width = static_cast<int>(width.Value);
	}
	if (ok == true) {
		auto height = Validators::Server::valid_heigth_or_weight(opts.values[static_cast<size_t>(Opt::Server::Id::Height)].values, &errors);
		ok &= height.Ok;
		if (ok)
			args.height = static_cast<int>(height.Value);
	}
	if (ok == true) {
		auto time = Validators::Server::time(opts.values[static_cast<size_t>(Opt::Server::Id::Time)].values, &errors);
		ok &= time.Ok;
		if (ok)
			args.time = static_cast<int>(time.Value);
	}
	if (ok == true) {
		auto clients = Validators::Server::clients(opts.values[static_cast<size_t>(Opt::Server::Id::Clients)].values, &errors);
		ok &= clients.Ok;
		if (ok)
			args.clients = static_cast<int>(clients.Value);
	}
	if (ok == true) {
		ok &= Validators::Server::teams(opts.values[static_cast<size_t>(Opt::Server::Id::Teams)].values, static_cast<size_t>(args.clients), &errors);
		if (ok)
			args.teams = opts.values[static_cast<size_t>(Opt::Server::Id::Teams)].values;
	}
	if (ok == false)
	{
		std::cerr << "Validation Error" << std::endl;
		for (auto& e : errors)
			std::cerr << e << std::endl;
  }
  	for (auto& e : opts.values[static_cast<size_t>(Opt::Server::Id::Teams)].values)
      std::cout <<  "TeamName: " << e << std::endl;

	return (ok == false || !errors.empty());
}
