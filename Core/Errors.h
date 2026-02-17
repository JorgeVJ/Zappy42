#pragma once
#include <string_view>

namespace Errors {
	constexpr std::string_view Unexpected   = "Unexpected error";

	namespace Exceptions {
		constexpr std::string_view VectorPushBack   = "Vector Push Back exceptions";
	}

	namespace CLI {
		constexpr std::string_view UnknownOption    = "Unknown option";
		constexpr std::string_view RepeatOption     = "Repeated option";
		constexpr std::string_view MissingOption    = "Missing option";
		constexpr std::string_view TooManyArguments = "Too many arguments";
		constexpr std::string_view MissingPort      = "Missing -p <port> argument";
		constexpr std::string_view InvalidArity     = "Invalid argument arity";
	}

	namespace Parser {
		constexpr std::string_view InvalidToken     = "Invalid token";
		constexpr std::string_view UnexpectedEOF    = "Unexpected end of input";
	}

	namespace Validation {

		constexpr std::string_view MissValue            = "Missing Value";
		constexpr std::string_view InvalidInteger       = "Invalid integer";
		constexpr std::string_view InvalidIntegerFormat = "Invalid integer format";
		constexpr std::string_view InvalidPort          = "Invalid port number";
		namespace Server {
			constexpr std::string_view InvalidHeightorWidth = "Invalid Height or Width number";
			constexpr std::string_view DuplicateTeamName    = "Invalid Duplicate Team Name";
			constexpr std::string_view Time          = "Invalid Time Nbr";
			constexpr std::string_view Clients     = "Invalid Client Nbr";
			constexpr std::string_view InvalidTeamNbr     = "Invalid Team Nbr";
			constexpr std::string_view InvalidTeamLen     = "Invalid Team Len";


		}
	}
}
