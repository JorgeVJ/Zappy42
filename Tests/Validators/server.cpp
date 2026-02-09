#include <iostream>
#include "GetOpt.h"

int main(int argc, char** argv) {
    ErrorReporter errors;


    Opt::GetOpt<Opt::Server::Opt> opts(
        std::span{Opt::Server::specs},
        std::span{Opt::Server::key_table},
        errors);

    bool ok = opts.parse(argc, argv);

	if (ok == false)
    {
      std::cerr << "Errors Validation" << std::endl;
      for (auto& e : errors.all())
		  std::cerr << e << std::endl;
      ok = true;
    }
	errors.clean();
     ok &= opts.validate();
    if (opts.getValues(Opt::Server::Opt::Port).has_values()) {
		std::cout << "Port:";
		for (auto i : opts.getValues(Opt::Server::Opt::Port).values)
			std::cout << " " << i;
		std::cout << " size: " << opts.getValues(Opt::Server::Opt::Port).values.size() << std::endl;
    }
    if (opts.getValues(Opt::Server::Opt::Width).has_values()) {
		std::cout << "Port:";
		for (auto i : opts.getValues(Opt::Server::Opt::Port).values)
			std::cout << " " << i;
		std::cout << " size: " << opts.getValues(Opt::Server::Opt::Port).values.size() << std::endl;
    }
    if (opts.getValues(Opt::Server::Opt::Height).has_values()) {
		std::cout << "Height:";
		for (auto i : opts.getValues(Opt::Server::Opt::Height).values)
			std::cout << " " << i;
		std::cout << " size: " << opts.getValues(Opt::Server::Opt::Height).values.size() << std::endl;
	}
    if (opts.getValues(Opt::Server::Opt::Teams).has_values()) {
	std::cout << "Teams:";
		for (auto i : opts.getValues(Opt::Server::Opt::Teams).values)
			std::cout << " " << i;
		std::cout << " size: " << opts.getValues(Opt::Server::Opt::Teams).values.size() << std::endl;
    }
    if (opts.getValues(Opt::Server::Opt::Clients).has_values()) {
			std::cout << "Clients:";
		for (auto i : opts.getValues(Opt::Server::Opt::Clients).values)
			std::cout << " " << i;
		std::cout << " size: " << opts.getValues(Opt::Server::Opt::Clients).values.size() << std::endl;
    }
    if (opts.getValues(Opt::Server::Opt::Time).has_values()) {
			std::cout << "Time:";
		for (auto i : opts.getValues(Opt::Server::Opt::Time).values)
			std::cout << " " << i;
		std::cout << " size: " << opts.getValues(Opt::Server::Opt::Time).values.size() << std::endl;
    }
    if (ok == false)
    {
      std::cerr << "Errors Validation" << std::endl;
      for (auto& e : errors.all())
		  std::cerr << e.msg << std::endl;
      return 1;
    }
    return (0);
}
