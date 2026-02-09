#include <iostream>
#include "GetOpt.h"

int main(int argc, char** argv) {
    ErrorReporter errors;


    Opt::GetOpt<Opt::Client::Opt> opts(
        std::span{Opt::Client::specs},
        std::span{Opt::Client::key_table},
        errors);

    bool ok = opts.parse(argc, argv);
	if (ok == false)
		{
			std::cerr << "Parsing Errors " << std::endl;
			for (auto& e : errors.all())
				std::cerr << e << std::endl;
			ok = true;
		}
	//Clean the std<vector>;
	errors.clean();

	ok &= opts.validate();


    if (opts.get(Opt::Client::Opt::Port).has_values()) {
		std::cout << "Port:";
		for (auto s : opts.get(Opt::Client::Opt::Port).values)
			std::cout << " " << s;
		std::cout <<  opts.get(Opt::Client::Opt::Port).values[0]  << " size: " << opts.get(Opt::Client::Opt::Port).values.size() << std::endl;
    }
    if (opts.get(Opt::Client::Opt::Teams).has_values()) {
		std::cout << "Team:";
		for (auto s : opts.get(Opt::Client::Opt::Teams).values)
			std::cout << " " << s ;
		std::cout << " size: " << opts.get(Opt::Client::Opt::Teams).values.size() << std::endl;
    }
    if (opts.get(Opt::Client::Opt::Host).has_values()) {
		std::cout << "Hostname:";
		for (auto s : opts.get(Opt::Client::Opt::Host).values)
			std::cout << " " << s ;
		std::cout << " size: " << opts.get(Opt::Client::Opt::Host).values.size() << std::endl;
    }
    if (ok == false)
    {
      std::cerr << "Validation Errors" << std::endl;
      for (auto& e : errors.all())
        std::cerr << e.msg << "\n";
      return (1);
    }
    return (0);
}
