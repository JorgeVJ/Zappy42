#include "ServerSimple.h"
#include "serveroptions.h"
#include "servervalidators.h"
#include "ArgValidation.h"
#include "GetOpt.h"

/// <summary>
/// Zappy Game Server - Main Entry Point
///
/// Simple, clean implementation:
/// - Parse arguments with GetOpt
/// - Validate with Result<T>
/// - Create and run Server
///
/// Usage:
///   zappy_server -p <port> -x <width> -y <height> -t <time> -c <clients> -n <team1> [<team2> ...]
///
/// Example:
///   zappy_server -p 12345 -x 20 -y 20 -t 60 -c 10 -n TeamA TeamB
/// </summary>
int main(int argc, char** argv)
{
	// ========================================================================
	// PLATFORM SOCKET INIT (Windows requiere WSAStartup antes de usar sockets)
	// ========================================================================
#ifdef _WIN32
	WSADATA wsaData{};
	if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
	{
		std::cerr << "WSAStartup() failed" << std::endl;
		return (1);
	}
#endif

	// ========================================================================
	// PARSE ARGUMENTS
	// ========================================================================

	std::vector<std::string_view> errors;

	Opt::GetOpt<Opt::Server::Id> opts(
									  std::span<const Opt::Spec> {Opt::Server::specs},
									  std::span<const Opt::KeyEntry<Opt::Server::Id>> {Opt::Server::key_table});

	bool ok = opts.parse(argc, argv, &errors);

	if (!ok || argc == 1)
	{
		std::cerr << "Parsing Error:" << std::endl;
		for (const auto& e : errors)
			std::cerr << "  " << e << std::endl;
		std::cerr << "\nUsage: " << argv[0]
			      << " -p <port> -x <width> -y <height> "
			      << "-t <time> -c <clients> -n <team1> [<team2> ...]" << std::endl;
		return (1);
	}

	// ========================================================================
	// VALIDATE ARITY
	// ========================================================================

	ok = validate_arity(opts.values, opts.specs, &errors);
	if (!ok)
	{
		std::cerr << "Arity Validation Error:" << std::endl;
		for (const auto& e : errors)
			std::cerr << "  " << e << std::endl;
		return (1);
	}

	// ========================================================================
	// VALIDATE ARGUMENTS
	// ========================================================================

	Opt::Server::Args args{};
	errors.clear();
	auto validationResult = ArgValidation::ValidateServerArgs(opts, args, &errors);
	if (!validationResult.Ok)
	{
		std::cerr << "Argument Validation Error:" << std::endl;
		for (const auto& e : errors)
			std::cerr << "  " << e << std::endl;
		std::cerr << "  " << validationResult.Message << std::endl;
		return (1);
	}

	// ========================================================================
	// CREATE AND RUN SERVER
	// ========================================================================

	Server server(args);

	if (!server.Initialize())
	{
		std::cerr << "Failed to initialize server" << std::endl;
		return (1);
	}

	int exitCode = server.Run();

	std::cout << "\nServer shutdown complete." << std::endl;

#ifdef _WIN32
	WSACleanup();
#endif

	return (exitCode);
}
