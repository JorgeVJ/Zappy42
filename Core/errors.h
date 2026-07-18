#pragma once
#include <string>

namespace Messages {
	namespace Game  {
		const std::string Welcome = "BIENVENUE\n";
		namespace Player {

		}
		namespace Monitor {
		}
	}
	namespace Server {
	}
}

namespace Errors {
	const std::string Unexpected   = "Unexpected error";

	namespace Exceptions {
		const std::string VectorPushBack   = "Vector Push Back exceptions";
	}

	namespace CLI {
		const std::string UnknownOption    = "Unknown option";
		const std::string RepeatOption     = "Repeated option";
		const std::string MissingOption    = "Missing option";
		const std::string TooManyArguments = "Too many arguments";
		const std::string MissingPort      = "Missing -p <port> argument";
		const std::string InvalidArity     = "Invalid argument arity";
	}

	namespace Parser {
		const std::string InvalidToken     = "Invalid token";
		const std::string UnexpectedEOF    = "Unexpected end of input";
	}

	namespace Validation {

		const std::string MissValue            = "Missing Value";
		const std::string InvalidInteger       = "Invalid integer";
		const std::string InvalidIntegerFormat = "Invalid integer format";
		const std::string InvalidPort          = "Invalid port number";
    const std::string InvalidTeamLen     = "Invalid Team Len";
		namespace Server {
			const std::string InvalidHeightorWidth = "Invalid Height or Width number";
			const std::string DuplicateTeamName    = "Invalid Duplicate Team Name";
			const std::string Time          = "Invalid Time Nbr";
			const std::string Clients     = "Invalid Client Nbr";
			const std::string InvalidTeamNbr     = "Invalid Team Nbr";
		}
	}
}
